using Microsoft.EntityFrameworkCore;
using SangRental.Application.CatalogFleet;
using SangRental.Domain.CatalogFleet;

namespace SangRental.Infrastructure.Persistence;

public sealed partial class CatalogFleetCommandService
{
    public Task ActivateVehicleAsync(long vehicleId, ChangeVehicleLifecycleRequest request, byte[] expectedVersion,
        CancellationToken ct = default) => ChangeLifecycleAsync(vehicleId, "activate", request, expectedVersion, ct);

    public Task RetireVehicleAsync(long vehicleId, ChangeVehicleLifecycleRequest request, byte[] expectedVersion,
        CancellationToken ct = default) => ChangeLifecycleAsync(vehicleId, "retire", request, expectedVersion, ct);

    public Task RestoreVehicleAsync(long vehicleId, ChangeVehicleLifecycleRequest request, byte[] expectedVersion,
        CancellationToken ct = default) => ChangeLifecycleAsync(vehicleId, "restore", request, expectedVersion, ct);

    private async Task ChangeLifecycleAsync(long vehicleId, string action, ChangeVehicleLifecycleRequest request,
        byte[] expectedVersion, CancellationToken ct)
    {
        var actor = RequireActor(a => a.IsAdmin);
        CatalogFleetSetupService.Validate(request);
        if (vehicleId <= 0 || expectedVersion is not { Length: 8 })
            throw new System.ComponentModel.DataAnnotations.ValidationException("vehicle.invalid_version");
        if (action != "retire" && !request.ConfirmInspection)
            throw new CatalogFleetRuleException("vehicle.inspection_confirmation_required");

        await InTransactionAsync(async () =>
        {
            // Use the same lock as maintenance so release cannot race retirement or reactivation.
            await LockVehicleMaintenanceAsync(vehicleId, ct);
            var vehicle = await db.Set<PhysicalVehicle>().SingleOrDefaultAsync(v => v.Id == vehicleId, ct)
                ?? throw new CatalogFleetRuleException("vehicle.not_found");
            await db.Entry(vehicle).ReloadAsync(ct);
            CheckVersion(vehicle.RowVersion, expectedVersion);
            var requiredState = action switch { "activate" => "Draft", "retire" => "Active", _ => "Retired" };
            if (vehicle.RecordLifecycle != requiredState)
                throw new CatalogFleetRuleException("vehicle.invalid_lifecycle_transition");

            var now = clock.GetUtcNow().UtcDateTime;
            if (action == "retire")
            {
                // Do not strand a maintenance episode: release requires an Active vehicle.
                if (await db.Set<MaintenanceEpisode>().AnyAsync(e => e.PhysicalVehicleId == vehicleId
                    && e.StatusCode != "Completed" && e.StatusCode != "Cancelled", ct))
                    throw new CatalogFleetRuleException("vehicle.maintenance_open");
                vehicle.RecordLifecycle = "Retired";
                vehicle.RetiredAtUtc = now;
                vehicle.RetirementReason = request.Reason.Trim();
                vehicle.ReadyForRentAtUtc = null;
            }
            else
            {
                if (vehicle.OperationalStatus is "LostLocked" or "AccidentLocked")
                    throw new CatalogFleetRuleException("vehicle.incident_locked");
                if (action == "activate" && vehicle.OperationalStatus != "PendingInspection")
                    throw new CatalogFleetRuleException("vehicle.invalid_lifecycle_transition");
                if (!await db.Set<VehicleModel>().AnyAsync(m => m.Id == vehicle.VehicleModelId
                    && m.LifecycleStatus == "Active", ct))
                    throw new CatalogFleetRuleException("vehicle.model_unavailable");
                if (await db.Set<MaintenanceEpisode>().AnyAsync(e => e.PhysicalVehicleId == vehicleId
                    && e.StatusCode != "Completed" && e.StatusCode != "Cancelled" && e.ScheduledStartUtc <= now, ct))
                    throw new CatalogFleetRuleException("vehicle.maintenance_open");

                db.Set<VehicleInspection>().Add(new VehicleInspection
                {
                    PhysicalVehicleId = vehicleId,
                    InspectionTypeCode = action == "activate" ? "InitialReadiness" : "Reactivation",
                    ResultCode = "Passed", InspectedAtUtc = now, InspectorReference = actor.Reference,
                    Findings = request.Reason.Trim(), CreatedAtUtc = now, CreatedByReference = actor.Reference
                });
                if (vehicle.OperationalStatus != "Ready")
                    AddStatusHistory(vehicleId, vehicle.OperationalStatus, "Ready", request.Reason.Trim(), actor, now);
                vehicle.RecordLifecycle = "Active";
                vehicle.OperationalStatus = "Ready";
                vehicle.ReadyForRentAtUtc = now;
                vehicle.RetiredAtUtc = null;
                vehicle.RetirementReason = null;
            }
            vehicle.UpdatedAtUtc = now;
            vehicle.UpdatedByReference = actor.Reference;
            AddAudit("PhysicalVehicle", vehicleId, action switch
            {
                "activate" => "Activated", "retire" => "Retired", _ => "Restored"
            }, actor, request.Reason.Trim());
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }
}
