using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SangRental.Application.CatalogFleet;
using SangRental.Domain.CatalogFleet;

namespace SangRental.Infrastructure.Persistence;

public sealed class CatalogFleetSetupService(SangRentalDbContext db, ICatalogFleetActorSource actorSource, TimeProvider clock)
{
    public Task<long> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct) => WriteAsync(async actor =>
    {
        Validate(request);
        var category = new VehicleCategory { Code = request.Code, VehicleFamilyCode = request.VehicleFamilyCode,
            CreatedByReference = actor.Reference, CreatedAtUtc = clock.GetUtcNow().UtcDateTime };
        db.Add(category);
        await db.SaveChangesAsync(ct);
        db.Add(new VehicleCategoryTranslation { VehicleCategoryId = category.Id, Locale = "vi",
            Name = request.NameVi.Trim(), Slug = request.Code });
        return ("VehicleCategory", category.Id);
    }, ct);

    public Task<long> CreateBrandAsync(CreateBrandRequest request, CancellationToken ct) => WriteAsync(async actor =>
    {
        Validate(request);
        var brand = new VehicleBrand { Code = request.Code, DisplayName = request.DisplayName.Trim(),
            CreatedByReference = actor.Reference, CreatedAtUtc = clock.GetUtcNow().UtcDateTime };
        db.Add(brand);
        await db.SaveChangesAsync(ct);
        return ("VehicleBrand", brand.Id);
    }, ct);

    public Task<long> CreateModelAsync(CreateModelRequest request, CancellationToken ct) => WriteAsync(async actor =>
    {
        Validate(request);
        if (!await db.Set<VehicleCategory>().AnyAsync(c => c.Id == request.CategoryId && c.LifecycleStatus == "Active", ct)
            || !await db.Set<VehicleBrand>().AnyAsync(b => b.Id == request.BrandId && b.LifecycleStatus == "Active", ct))
            throw new CatalogFleetRuleException("catalog.parent_unavailable");
        var model = new VehicleModel { VehicleCategoryId = request.CategoryId, VehicleBrandId = request.BrandId,
            Code = request.Code, CommercialName = request.CommercialName.Trim(), CreatedByReference = actor.Reference,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime };
        db.Add(model);
        await db.SaveChangesAsync(ct);
        return ("VehicleModel", model.Id);
    }, ct);

    public Task<long> CreateDraftVehicleAsync(CreateVehicleRequest request, CancellationToken ct) => WriteAsync(async actor =>
    {
        Validate(request);
        await SqlApplicationLock.AcquireAsync(db, "SangRental:Fleet:VehicleCreate", ct);
        if (!await db.Set<VehicleModel>().AnyAsync(m => m.Id == request.ModelId && m.LifecycleStatus == "Active", ct))
            throw new CatalogFleetRuleException("vehicle.model_unavailable");
        var vin = string.IsNullOrWhiteSpace(request.Vin) ? null : request.Vin.Trim();
        var normalized = vin?.ToUpperInvariant();
        if (normalized is not null && await db.Set<PhysicalVehicle>().AnyAsync(v => v.NormalizedVinOrChassisNumber == normalized, ct)
            && string.IsNullOrWhiteSpace(request.DuplicateVinReason))
            throw new CatalogFleetRuleException("vehicle.duplicate_vin_reason_required");
        var vehicle = new PhysicalVehicle { VehicleModelId = request.ModelId, AssetCode = request.AssetCode.Trim(),
            VinOrChassisNumber = vin, NormalizedVinOrChassisNumber = normalized,
            CreatedByReference = actor.Reference, CreatedAtUtc = clock.GetUtcNow().UtcDateTime };
        db.Add(vehicle);
        await db.SaveChangesAsync(ct);
        return ("PhysicalVehicle", vehicle.Id);
    }, ct, request.DuplicateVinReason);

    private async Task<long> WriteAsync(Func<CatalogFleetActor, Task<(string Entity, long Id)>> action, CancellationToken ct, string? reason = null)
    {
        var actor = actorSource.GetRequiredActor();
        if (!actor.IsAdmin) throw new UnauthorizedAccessException();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var (entity, id) = await action(actor);
        db.Add(new CatalogFleetAuditEvent { EntityType = entity, EntityId = id, ActionCode = "DraftCreated",
            ActorReference = actor.Reference, OccurredAtUtc = clock.GetUtcNow().UtcDateTime, CorrelationId = Guid.NewGuid(), Reason = reason });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return id;
    }

    public static void Validate(object request) => Validator.ValidateObject(request, new ValidationContext(request), true);
}
