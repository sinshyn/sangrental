using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SangRental.Application.CatalogFleet;
using SangRental.Domain.CatalogFleet;

namespace SangRental.Infrastructure.Persistence;

public sealed partial class CatalogFleetCommandService
{
    public async Task UpdateVehicleProfileAsync(long vehicleId, UpdateVehicleProfileRequest request,
        byte[] expectedVersion, CancellationToken ct = default)
    {
        var actor = RequireActor(a => a.IsAdmin);
        CatalogFleetSetupService.Validate(request);
        if (request.ManufacturingYear is <= 0) throw new ValidationException("vehicle.invalid_year");
        await InTransactionAsync(async () =>
        {
            // Same order as create: serialize normalized VIN checks before touching the vehicle.
            await SqlApplicationLock.AcquireAsync(db, "SangRental:Fleet:VehicleCreate", ct);
            var vehicle = await LoadVehicleForUpdateAsync(vehicleId, expectedVersion, ct);
            if (!await db.Set<VehicleModel>().AnyAsync(x => x.Id == request.ModelId && x.LifecycleStatus == "Active", ct))
                throw new CatalogFleetRuleException("vehicle.model_unavailable");
            var vin = OptionalText(request.Vin);
            var normalized = vin?.ToUpperInvariant();
            if (normalized != vehicle.NormalizedVinOrChassisNumber && normalized is not null
                && await db.Set<PhysicalVehicle>().AnyAsync(x => x.Id != vehicleId && x.NormalizedVinOrChassisNumber == normalized, ct)
                && string.IsNullOrWhiteSpace(request.DuplicateVinReason))
                throw new CatalogFleetRuleException("vehicle.duplicate_vin_reason_required");

            var previousModel = vehicle.VehicleModelId;
            vehicle.VehicleModelId = request.ModelId;
            vehicle.ColorName = OptionalText(request.ColorName);
            vehicle.ManufacturingYear = request.ManufacturingYear;
            vehicle.VinOrChassisNumber = vin;
            vehicle.NormalizedVinOrChassisNumber = normalized;
            vehicle.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
            vehicle.UpdatedByReference = actor.Reference;
            AddAudit("PhysicalVehicle", vehicleId, "ProfileUpdated", actor, request.Reason.Trim());
            if (previousModel != request.ModelId)
                AddAudit("PhysicalVehicle", vehicleId, "ModelReassigned", actor,
                    $"{previousModel}->{request.ModelId}");
            if (normalized is not null && !string.IsNullOrWhiteSpace(request.DuplicateVinReason))
                AddAudit("PhysicalVehicle", vehicleId, "VinOverrideAcknowledged", actor, request.DuplicateVinReason.Trim());
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }

    public async Task ChangeVehicleRegistrationAsync(long vehicleId, ChangeVehicleRegistrationRequest request,
        byte[] expectedVersion, CancellationToken ct = default)
    {
        var actor = RequireActor(a => a.IsAdmin);
        CatalogFleetSetupService.Validate(request);
        // Comparison format only, not legal validation of a country's registration rules.
        var normalized = new string(request.Plate.Where(c => c is not (' ' or '.' or '-')).ToArray()).ToUpperInvariant();
        if (normalized.Length == 0 || normalized.Any(c => !char.IsAsciiLetterOrDigit(c)))
            throw new ValidationException("vehicle.invalid_plate");
        await InTransactionAsync(async () =>
        {
            var vehicle = await LoadVehicleForUpdateAsync(vehicleId, expectedVersion, ct);
            await SqlApplicationLock.AcquireAsync(db, "SangRental:Fleet:Registrations", ct);
            var current = await db.Set<VehicleRegistration>().SingleOrDefaultAsync(x => x.PhysicalVehicleId == vehicleId
                && x.ValidToUtc == null, ct);
            if (current is not null && current.RegistrationJurisdictionCode == request.JurisdictionCode
                && current.NormalizedPlate == normalized)
                throw new CatalogFleetRuleException("vehicle.registration_unchanged");
            if (await db.Set<VehicleRegistration>().AnyAsync(x => x.PhysicalVehicleId != vehicleId
                && x.ValidToUtc == null && x.RegistrationJurisdictionCode == request.JurisdictionCode
                && x.NormalizedPlate == normalized, ct))
                throw new CatalogFleetRuleException("vehicle.plate_in_use");
            var now = clock.GetUtcNow().UtcDateTime;
            if (current is not null)
            {
                if (now <= current.ValidFromUtc) throw new CatalogFleetRuleException("vehicle.registration_time_conflict");
                current.ValidToUtc = now;
                current.ClosedAtUtc = now;
                current.ClosedByReference = actor.Reference;
                current.ClosureReason = request.Reason.Trim();
                // Close first so the filtered unique current/unit index is respected.
                await db.SaveChangesAsync(ct);
            }
            db.Set<VehicleRegistration>().Add(new VehicleRegistration
            {
                PhysicalVehicleId = vehicleId, RegistrationJurisdictionCode = request.JurisdictionCode,
                PlateDisplay = request.Plate.Trim(), NormalizedPlate = normalized, ValidFromUtc = now,
                CreatedAtUtc = now, CreatedByReference = actor.Reference
            });
            vehicle.UpdatedAtUtc = now;
            vehicle.UpdatedByReference = actor.Reference;
            AddAudit("PhysicalVehicle", vehicleId, "RegistrationChanged", actor, request.Reason.Trim());
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }

    private async Task<PhysicalVehicle> LoadVehicleForUpdateAsync(long vehicleId, byte[] expectedVersion, CancellationToken ct)
    {
        if (vehicleId <= 0 || expectedVersion is not { Length: 8 }) throw new ValidationException("vehicle.invalid_version");
        await LockVehicleMaintenanceAsync(vehicleId, ct);
        var vehicle = await db.Set<PhysicalVehicle>().SingleOrDefaultAsync(x => x.Id == vehicleId, ct)
            ?? throw new CatalogFleetRuleException("vehicle.not_found");
        await db.Entry(vehicle).ReloadAsync(ct);
        CheckVersion(vehicle.RowVersion, expectedVersion);
        return vehicle;
    }

    private static string? OptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
