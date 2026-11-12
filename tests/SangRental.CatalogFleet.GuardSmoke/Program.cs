using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SangRental.Application.CatalogFleet;
using SangRental.Domain.CatalogFleet;
using SangRental.Infrastructure.Persistence;
using SangRental.Infrastructure.Security;

try
{
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("backend/SangRental.Api/appsettings.Development.json", optional: false)
    .AddUserSecrets("SangRental-Api-development")
    .AddEnvironmentVariables()
    .Build();
// The same local allowlist as the API test mode; never write to a remote server.
configuration.UseLocalTestDatabase();
var connection = new SqlConnectionStringBuilder(configuration.GetConnectionString("SangRental"));
var options = new DbContextOptionsBuilder<SangRentalDbContext>()
    .UseSqlServer(connection.ConnectionString).Options;
var actor = new FixedActorSource(new CatalogFleetActor("integration-smoke", true, true, true, true));
var token = Guid.NewGuid().ToString("N")[..12];
var checks = 0;

await using (var db = new SangRentalDbContext(options))
await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable))
{
    var now = DateTime.UtcNow;
    var category = new VehicleCategory
    {
        Code = $"test-{token}", VehicleFamilyCode = "Motorcycle",
        PublicationStatus = "Published", CreatedByReference = actor.GetRequiredActor().Reference
    };
    var brand = new VehicleBrand
    {
        Code = $"test-{token}", DisplayName = "Integration Test",
        CreatedByReference = actor.GetRequiredActor().Reference
    };
    db.Set<VehicleCategory>().Add(category);
    db.Set<VehicleBrand>().Add(brand);
    await db.SaveChangesAsync();
    var model = new VehicleModel
    {
        VehicleCategoryId = category.Id, VehicleBrandId = brand.Id,
        Code = $"test-{token}", CommercialName = "Integration Test",
        CreatedByReference = actor.GetRequiredActor().Reference
    };
    db.Set<VehicleModel>().Add(model);
    await db.SaveChangesAsync();
    var service = new CatalogFleetCommandService(db, actor, TimeProvider.System);

    await ExpectRule("catalog.publication_incomplete", () => service.PublishModelAsync(model.Id));
    checks++;

    var unauthorized = new CatalogFleetCommandService(db,
        new FixedActorSource(new CatalogFleetActor("unauthorized-test", false, false, false, false)),
        TimeProvider.System);
    await ExpectUnauthorized(() => unauthorized.CreateReadyVehicleAsync(new PhysicalVehicle
    {
        VehicleModelId = model.Id, AssetCode = $"DENIED-{token}"
    }));
    checks++;

    var vehicleId = await service.CreateReadyVehicleAsync(new PhysicalVehicle
    {
        VehicleModelId = model.Id, AssetCode = $"TEST-{token}",
        VinOrChassisNumber = $"VIN-{token}", NormalizedVinOrChassisNumber = $"VIN-{token}"
    });
    var ready = await db.Set<PhysicalVehicle>().AsNoTracking()
        .SingleAsync(v => v.Id == vehicleId);
    Ensure(ready.RecordLifecycle == "Active" && ready.OperationalStatus == "Ready", "Ready activation");
    Ensure(await db.Set<VehicleInspection>().CountAsync(i => i.PhysicalVehicleId == vehicleId
        && i.InspectionTypeCode == "InitialReadiness" && i.ResultCode == "Passed") == 1, "Initial inspection");
    checks += 2;
    await ExpectRule("vehicle.not_deletable_draft", () => service.DeleteDraftVehicleAsync(vehicleId));
    checks++;
    var draftVehicle = new PhysicalVehicle
    {
        VehicleModelId = model.Id, AssetCode = $"DRAFT-{token}",
        CreatedByReference = actor.GetRequiredActor().Reference
    };
    db.Set<PhysicalVehicle>().Add(draftVehicle);
    await db.SaveChangesAsync();
    await service.DeleteDraftVehicleAsync(draftVehicle.Id);
    Ensure(!await db.Set<PhysicalVehicle>().AsNoTracking().AnyAsync(v => v.Id == draftVehicle.Id),
        "Unreferenced draft vehicle deleted");
    checks++;

    var draftModel = new VehicleModel
    {
        VehicleCategoryId = category.Id, VehicleBrandId = brand.Id,
        Code = $"draft-{token}", CommercialName = "Draft Test",
        CreatedByReference = actor.GetRequiredActor().Reference
    };
    db.Set<VehicleModel>().Add(draftModel);
    await db.SaveChangesAsync();
    await service.DeleteDraftModelAsync(draftModel.Id);
    Ensure(!await db.Set<VehicleModel>().AsNoTracking().AnyAsync(m => m.Id == draftModel.Id),
        "Unreferenced draft model deleted");
    checks++;

    await ExpectRule("vehicle.duplicate_vin_reason_required", () =>
        service.CreateReadyVehicleAsync(new PhysicalVehicle
        {
            VehicleModelId = model.Id, AssetCode = $"DUP-{token}",
            VinOrChassisNumber = $"VIN-{token}", NormalizedVinOrChassisNumber = $"VIN-{token}"
        }));
    checks++;
    var duplicateId = await service.CreateReadyVehicleAsync(new PhysicalVehicle
    {
        VehicleModelId = model.Id, AssetCode = $"DUP-{token}",
        VinOrChassisNumber = $"VIN-{token}", NormalizedVinOrChassisNumber = $"VIN-{token}"
    }, "Verified test exception");
    Ensure(duplicateId > 0, "Admin duplicate VIN override");
    checks++;

    var lifecycle = new PhysicalVehicle { VehicleModelId = model.Id, AssetCode = $"LIFE-{token}",
        CreatedByReference = actor.GetRequiredActor().Reference };
    db.Add(lifecycle);
    await db.SaveChangesAsync();
    var inspectionRequest = new ChangeVehicleLifecycleRequest("Guard smoke inspection", true);
    var draftVersion = lifecycle.RowVersion.ToArray();
    await ExpectUnauthorized(() => unauthorized.ActivateVehicleAsync(lifecycle.Id, inspectionRequest, draftVersion));
    await ExpectRule("vehicle.inspection_confirmation_required", () => service.ActivateVehicleAsync(lifecycle.Id,
        new("Not checked"), draftVersion));
    checks += 2;
    await service.ActivateVehicleAsync(lifecycle.Id, inspectionRequest, draftVersion);
    Ensure(lifecycle.RecordLifecycle == "Active" && lifecycle.OperationalStatus == "Ready", "Existing draft activated");
    checks++;
    await service.RetireVehicleAsync(lifecycle.Id, new("Guard retirement"), lifecycle.RowVersion.ToArray());
    Ensure(lifecycle.RecordLifecycle == "Retired" && lifecycle.ReadyForRentAtUtc == null, "Retirement clears readiness");
    checks++;
    await ExpectRule("vehicle.inspection_confirmation_required", () => service.RestoreVehicleAsync(lifecycle.Id,
        new("Not checked"), lifecycle.RowVersion.ToArray()));
    checks++;
    model.LifecycleStatus = "Retired";
    await db.SaveChangesAsync();
    await ExpectRule("vehicle.model_unavailable", () => service.RestoreVehicleAsync(lifecycle.Id, inspectionRequest, lifecycle.RowVersion.ToArray()));
    model.LifecycleStatus = "Active";
    await db.SaveChangesAsync();
    checks++;
    await service.RestoreVehicleAsync(lifecycle.Id, inspectionRequest, lifecycle.RowVersion.ToArray());
    Ensure(await db.Set<VehicleInspection>().CountAsync(i => i.PhysicalVehicleId == lifecycle.Id) == 2, "Lifecycle inspection evidence retained");
    Ensure(await db.Set<CatalogFleetAuditEvent>().CountAsync(i => i.EntityType == "PhysicalVehicle" && i.EntityId == lifecycle.Id) == 3, "Lifecycle audit history retained");
    checks += 2;

    foreach (var status in new[] { "LostLocked", "AccidentLocked" })
    {
        var incidentVehicle = new PhysicalVehicle { VehicleModelId = model.Id, AssetCode = $"{status}-{token}",
            RecordLifecycle = "Retired", OperationalStatus = status, RetiredAtUtc = now,
            RetirementReason = "Incident test", CreatedByReference = actor.GetRequiredActor().Reference };
        db.Add(incidentVehicle);
        await db.SaveChangesAsync();
        await ExpectRule("vehicle.incident_locked", () => service.RestoreVehicleAsync(incidentVehicle.Id,
            inspectionRequest, incidentVehicle.RowVersion.ToArray()));
        Ensure(incidentVehicle.RecordLifecycle == "Retired" && incidentVehicle.OperationalStatus == status,
            "Restoration cannot bypass incident lock");
        checks += 2;
    }

    var planned = await service.ScheduleMaintenanceAsync(lifecycle.Id, now.AddDays(1), null, "Future maintenance");
    await ExpectRule("vehicle.maintenance_open", () => service.RetireVehicleAsync(lifecycle.Id,
        new("No stranded maintenance"), lifecycle.RowVersion.ToArray()));
    checks++;

    var episodeId = await service.ScheduleMaintenanceAsync(
        vehicleId, now.AddMinutes(-1), null, "Integration test");
    var maintainedVehicle = await db.Set<PhysicalVehicle>().SingleAsync(x => x.Id == vehicleId);
    await ExpectRule("vehicle.maintenance_open", () => service.RetireVehicleAsync(vehicleId,
        new("Open maintenance"), maintainedVehicle.RowVersion.ToArray()));
    checks++;
    await ExpectRule("maintenance.overlap", () => service.ScheduleMaintenanceAsync(
        vehicleId, now, null, "Overlapping test"));
    checks++;

    var locked = new PhysicalVehicle
    {
        VehicleModelId = model.Id, AssetCode = $"LOCK-{token}",
        RecordLifecycle = "Active", OperationalStatus = "LostLocked",
        CreatedByReference = actor.GetRequiredActor().Reference
    };
    db.Set<PhysicalVehicle>().Add(locked);
    await db.SaveChangesAsync();
    var lockedEpisodeId = await service.ScheduleMaintenanceAsync(
        locked.Id, DateTime.UtcNow.AddMinutes(-1), null, "Locked vehicle test");
    Ensure((await db.Set<PhysicalVehicle>().AsNoTracking().SingleAsync(v => v.Id == locked.Id))
        .OperationalStatus == "LostLocked", "Lost lock survives maintenance recording");
    await ExpectRule("maintenance.vehicle_locked", () => service.CompleteMaintenanceAsync(lockedEpisodeId));
    checks += 2;
    await ExpectRule("maintenance.checklist_incomplete", () => service.CompleteMaintenanceAsync(episodeId));
    checks++;

    var taskId = await service.AddMaintenanceTaskAsync(episodeId, null, "Kiểm tra thử");
    await service.SetMaintenanceTaskStatusAsync(taskId, "Completed", "Đạt", null);
    Ensure((await db.Set<MaintenanceChecklistItem>().AsNoTracking().SingleAsync(i => i.Id == taskId))
        .StatusCode == "Completed", "Checklist status");
    checks++;
    await service.CompleteMaintenanceAsync(episodeId);
    var completed = await db.Set<MaintenanceEpisode>().AsNoTracking().SingleAsync(e => e.Id == episodeId);
    ready = await db.Set<PhysicalVehicle>().AsNoTracking().SingleAsync(v => v.Id == vehicleId);
    Ensure(completed.ReleaseInspectionId is not null && completed.ReleasedAtUtc is not null
        && ready.OperationalStatus == "Ready", "Maintenance release");
    checks++;

    db.Set<VehicleCategoryTranslation>().Add(new VehicleCategoryTranslation
    {
        VehicleCategoryId = category.Id, Locale = "vi", Name = "Loại thử",
        Slug = $"category-{token}", TranslationStatus = "Reviewed",
        SourceRevision = category.ContentRevision,
        ReviewedByReference = actor.GetRequiredActor().Reference, ReviewedAtUtc = now
    });
    db.Set<VehicleModelTranslation>().Add(new VehicleModelTranslation
    {
        VehicleModelId = model.Id, Locale = "vi", DisplayName = "Mẫu thử",
        Slug = $"test-{token}", TranslationStatus = "Reviewed",
        SourceRevision = model.ContentRevision, ReviewedByReference = actor.GetRequiredActor().Reference,
        ReviewedAtUtc = now
    });
    db.Set<VehicleModelImage>().Add(new VehicleModelImage
    {
        VehicleModelId = model.Id, StorageKey = $"test/{token}", ContentHash = new byte[32],
        MediaType = "image/jpeg", Width = 1, Height = 1, IsVisible = true, IsPrimary = true,
        ApprovalStatus = "Approved", SourceReference = "integration-test",
        RightsReference = "integration-test", ApprovedAtUtc = now,
        ApprovedByReference = actor.GetRequiredActor().Reference,
        CreatedByReference = actor.GetRequiredActor().Reference
    });
    db.Set<VehicleModelSpecification>().Add(new VehicleModelSpecification
    {
        VehicleModelId = model.Id, HasAbs = true, VerificationStatus = "Verified",
        VerifiedAtUtc = now, VerifiedByReference = actor.GetRequiredActor().Reference,
        CreatedByReference = actor.GetRequiredActor().Reference
    });
    await db.SaveChangesAsync();
    await ExpectRule("catalog.specification_unverified", () => service.PublishModelAsync(model.Id));
    checks++;
    var spec = await db.Set<VehicleModelSpecification>().SingleAsync(s => s.VehicleModelId == model.Id);
    spec.HasAbsSourceReference = "integration-test";
    await db.SaveChangesAsync();
    await service.PublishModelAsync(model.Id);
    Ensure((await db.Set<VehicleModel>().AsNoTracking().SingleAsync(m => m.Id == model.Id)).PublicationStatus
        == "Published", "Publication gate");
    checks++;
    Ensure(await db.Set<CatalogFleetAuditEvent>().CountAsync(a => a.ActorReference == "integration-smoke") >= 4,
        "Audit events");
    checks++;

    await transaction.RollbackAsync();
}

await using (var verification = new SangRentalDbContext(options))
{
    Ensure(!await verification.Set<VehicleCategory>().AnyAsync(c => c.Code == $"test-{token}"),
        "Rollback cleanup");
    checks++;
}
await using (var first = new SqlConnection(connection.ConnectionString))
await using (var second = new SqlConnection(connection.ConnectionString))
{
    await first.OpenAsync();
    await second.OpenAsync();
    await using var firstTransaction = (SqlTransaction)await first.BeginTransactionAsync();
    await using var secondTransaction = (SqlTransaction)await second.BeginTransactionAsync();
    var resource = $"SangRental:Maintenance:Smoke:{token}";
    Ensure(await AcquireLock(first, firstTransaction, resource, 1000) >= 0, "First maintenance lock");
    Ensure(await AcquireLock(second, secondTransaction, resource, 100) < 0, "Concurrent lock rejected");
    checks += 2;
    await firstTransaction.RollbackAsync();
    Ensure(await AcquireLock(second, secondTransaction, resource, 1000) >= 0, "Lock available after release");
    checks++;
    await secondTransaction.RollbackAsync();
}
Console.WriteLine($"PASS: {checks} Catalog/Fleet SQL guard checks; all test writes rolled back.");
}
catch (Exception error)
{
    Console.Error.WriteLine($"FAIL: Catalog/Fleet SQL guard checks ({error.GetType().Name}). No credentials or SQL payloads are printed.");
    Environment.ExitCode = 1;
}

static async Task<int> AcquireLock(SqlConnection connection, SqlTransaction transaction,
    string resource, int timeoutMs)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = "DECLARE @result int; EXEC @result = sp_getapplock " +
        "@Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', " +
        "@LockTimeout = @timeout; SELECT @result;";
    command.Parameters.AddWithValue("@resource", resource);
    command.Parameters.AddWithValue("@timeout", timeoutMs);
    return (int)(await command.ExecuteScalarAsync()
        ?? throw new InvalidOperationException("Lock result missing."));
}

static async Task ExpectRule(string code, Func<Task> action)
{
    try { await action(); }
    catch (CatalogFleetRuleException exception) when (exception.Code == code) { return; }
    throw new InvalidOperationException($"Expected rule {code}.");
}

static async Task ExpectUnauthorized(Func<Task> action)
{
    try { await action(); }
    catch (UnauthorizedAccessException) { return; }
    throw new InvalidOperationException("Expected authorization failure.");
}

static void Ensure(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"Check failed: {name}.");
}

sealed class FixedActorSource(CatalogFleetActor actor) : ICatalogFleetActorSource
{
    public CatalogFleetActor GetRequiredActor() => actor;
}
