using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SangRental.Application.CatalogFleet;
using SangRental.Domain.CatalogFleet;

namespace SangRental.Infrastructure.Persistence;

/// <summary>
/// Fleet write workflows. Booking coordination remains outside this slice.
/// </summary>
public sealed partial class CatalogFleetCommandService(
    SangRentalDbContext db,
    ICatalogFleetActorSource actorSource,
    TimeProvider clock)
{
    public async Task<long> CreateReadyVehicleAsync(
        PhysicalVehicle vehicle,
        string? duplicateVinReason = null,
        CancellationToken cancellationToken = default)
    {
        var actor = RequireActor(a => a.IsAdmin);
        if (vehicle.Id != 0 || string.IsNullOrWhiteSpace(vehicle.AssetCode))
            throw new CatalogFleetRuleException("vehicle.invalid");

        return await InTransactionAsync(async () =>
        {
            // Serializes VIN warnings across concurrent creates; VIN itself is deliberately not unique.
            await SqlApplicationLock.AcquireAsync(db, "SangRental:Fleet:VehicleCreate", cancellationToken);
            var modelExists = await db.Set<VehicleModel>().AnyAsync(
                m => m.Id == vehicle.VehicleModelId && m.LifecycleStatus == "Active", cancellationToken);
            if (!modelExists) throw new CatalogFleetRuleException("vehicle.model_unavailable");

            if (!string.IsNullOrWhiteSpace(vehicle.NormalizedVinOrChassisNumber))
            {
                var duplicate = await db.Set<PhysicalVehicle>().AnyAsync(
                    v => v.NormalizedVinOrChassisNumber == vehicle.NormalizedVinOrChassisNumber,
                    cancellationToken);
                if (duplicate && string.IsNullOrWhiteSpace(duplicateVinReason))
                    throw new CatalogFleetRuleException("vehicle.duplicate_vin_reason_required");
            }

            var now = clock.GetUtcNow().UtcDateTime;
            vehicle.RecordLifecycle = "Active";
            vehicle.OperationalStatus = "Ready";
            vehicle.ReadyForRentAtUtc = now;
            vehicle.CreatedByReference = actor.Reference;
            vehicle.CreatedAtUtc = now;
            db.Set<PhysicalVehicle>().Add(vehicle);
            await db.SaveChangesAsync(cancellationToken);

            db.Set<VehicleInspection>().Add(new VehicleInspection
            {
                PhysicalVehicleId = vehicle.Id,
                InspectionTypeCode = "InitialReadiness",
                ResultCode = "Passed",
                InspectedAtUtc = now,
                InspectorReference = actor.Reference,
                CreatedAtUtc = now,
                CreatedByReference = actor.Reference
            });
            db.Set<VehicleOperationalStatusHistory>().Add(new VehicleOperationalStatusHistory
            {
                PhysicalVehicleId = vehicle.Id,
                PreviousStatus = null,
                NewStatus = "Ready",
                OccurredAtUtc = now,
                Reason = "Initial readiness inspection passed",
                CorrelationId = Guid.NewGuid(),
                SourceCode = "CatalogFleet",
                CreatedAtUtc = now,
                CreatedByReference = actor.Reference
            });
            AddAudit("PhysicalVehicle", vehicle.Id, "CreatedReady", actor, duplicateVinReason);
            await db.SaveChangesAsync(cancellationToken);
            return vehicle.Id;
        }, cancellationToken);
    }

    public async Task<long> ScheduleMaintenanceAsync(
        long vehicleId,
        DateTime scheduledStartUtc,
        DateTime? scheduledEndUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var actor = RequireActor(a => a.IsAdmin || a.CanManageFleet);
        if (vehicleId <= 0 || scheduledStartUtc.Kind != DateTimeKind.Utc
            || scheduledEndUtc is { Kind: not DateTimeKind.Utc }
            || scheduledEndUtc <= scheduledStartUtc || string.IsNullOrWhiteSpace(reason))
            throw new CatalogFleetRuleException("maintenance.invalid_interval");

        return await InTransactionAsync(async () =>
        {
            await LockVehicleMaintenanceAsync(vehicleId, cancellationToken);
            var vehicle = await db.Set<PhysicalVehicle>().SingleOrDefaultAsync(v => v.Id == vehicleId, cancellationToken);
            if (vehicle is null || vehicle.RecordLifecycle != "Active")
                throw new CatalogFleetRuleException("maintenance.vehicle_unavailable");

            var upper = scheduledEndUtc ?? DateTime.MaxValue;
            var now = clock.GetUtcNow().UtcDateTime;
            var overlap = await db.Set<MaintenanceEpisode>().AnyAsync(
                e => e.PhysicalVehicleId == vehicleId && e.StatusCode != "Cancelled"
                     && e.ScheduledStartUtc < upper
                     && (e.ActualEndUtc != null ? e.ActualEndUtc > scheduledStartUtc
                         : e.ScheduledEndUtc == null || e.ScheduledEndUtc <= now || e.ScheduledEndUtc > scheduledStartUtc),
                cancellationToken);
            if (overlap) throw new CatalogFleetRuleException("maintenance.overlap");

            var episode = new MaintenanceEpisode
            {
                PhysicalVehicleId = vehicleId,
                ScheduledStartUtc = scheduledStartUtc,
                ScheduledEndUtc = scheduledEndUtc,
                StatusCode = scheduledStartUtc <= now ? "InProgress" : "Planned",
                Reason = reason.Trim(),
                ActualStartUtc = scheduledStartUtc <= now ? now : null,
                CreatedAtUtc = now,
                CreatedByReference = actor.Reference
            };
            db.Set<MaintenanceEpisode>().Add(episode);
            await db.SaveChangesAsync(cancellationToken);

            if (episode.StatusCode == "InProgress"
                && vehicle.OperationalStatus is not ("LostLocked" or "AccidentLocked"))
            {
                var previous = vehicle.OperationalStatus;
                vehicle.OperationalStatus = "Maintenance";
                vehicle.UpdatedAtUtc = now;
                vehicle.UpdatedByReference = actor.Reference;
                AddStatusHistory(vehicleId, previous, "Maintenance", "Maintenance started", actor, now);
            }
            AddAudit("MaintenanceEpisode", episode.Id, "Scheduled", actor, reason);
            await db.SaveChangesAsync(cancellationToken);
            return episode.Id;
        }, cancellationToken);
    }

    public async Task CompleteMaintenanceAsync(long episodeId, CancellationToken cancellationToken = default, byte[]? expectedVersion = null)
    {
        var actor = RequireActor(a => a.IsAdmin || a.CanApproveFleet);
        var vehicleId = await db.Set<MaintenanceEpisode>().AsNoTracking().Where(e => e.Id == episodeId)
            .Select(e => (long?)e.PhysicalVehicleId).SingleOrDefaultAsync(cancellationToken);
        if (vehicleId is null) throw new CatalogFleetRuleException("maintenance.not_open");
        await InTransactionAsync(async () =>
        {
            await LockVehicleMaintenanceAsync(vehicleId.Value, cancellationToken);
            var episode = await db.Set<MaintenanceEpisode>().SingleOrDefaultAsync(e => e.Id == episodeId, cancellationToken);
            if (episode is null || episode.StatusCode is "Completed" or "Cancelled")
                throw new CatalogFleetRuleException("maintenance.not_open");
            await db.Entry(episode).ReloadAsync(cancellationToken);
            if (episode.StatusCode is "Completed" or "Cancelled")
                throw new CatalogFleetRuleException("maintenance.not_open");
            CheckVersion(episode.RowVersion, expectedVersion);
            var vehicle = await db.Set<PhysicalVehicle>().SingleAsync(v => v.Id == episode.PhysicalVehicleId, cancellationToken);
            await db.Entry(vehicle).ReloadAsync(cancellationToken);
            if (vehicle.RecordLifecycle != "Active" || vehicle.OperationalStatus is "LostLocked" or "AccidentLocked")
                throw new CatalogFleetRuleException("maintenance.vehicle_locked");
            var now = clock.GetUtcNow().UtcDateTime;
            if (episode.ScheduledStartUtc > now)
                throw new CatalogFleetRuleException("maintenance.not_started");
            if (await db.Set<MaintenanceEpisode>().AnyAsync(e => e.PhysicalVehicleId == vehicle.Id
                && e.Id != episode.Id && e.StatusCode != "Completed" && e.StatusCode != "Cancelled"
                && e.ScheduledStartUtc <= now, cancellationToken))
                throw new CatalogFleetRuleException("maintenance.other_episode_open");
            var items = await db.Set<MaintenanceChecklistItem>()
                .Where(i => i.MaintenanceEpisodeId == episodeId).ToListAsync(cancellationToken);
            if (items.Count == 0 || items.Any(i => i.StatusCode is not ("Completed" or "Skipped")))
                throw new CatalogFleetRuleException("maintenance.checklist_incomplete");

            var inspection = new VehicleInspection
            {
                PhysicalVehicleId = vehicle.Id,
                InspectionTypeCode = "MaintenanceRelease",
                ResultCode = "Passed",
                InspectedAtUtc = now,
                InspectorReference = actor.Reference,
                CreatedAtUtc = now,
                CreatedByReference = actor.Reference
            };
            db.Set<VehicleInspection>().Add(inspection);
            await db.SaveChangesAsync(cancellationToken);

            episode.StatusCode = "Completed";
            episode.ActualStartUtc ??= now;
            episode.ActualEndUtc = now;
            episode.ReleasedAtUtc = now;
            episode.ReleasedByReference = actor.Reference;
            episode.ReleaseInspectionId = inspection.Id;
            episode.UpdatedAtUtc = now;
            episode.UpdatedByReference = actor.Reference;
            var previous = vehicle.OperationalStatus;
            vehicle.OperationalStatus = "Ready";
            vehicle.ReadyForRentAtUtc = now;
            vehicle.UpdatedAtUtc = now;
            vehicle.UpdatedByReference = actor.Reference;
            AddStatusHistory(vehicle.Id, previous, "Ready", "Maintenance release inspection passed", actor, now);
            AddAudit("MaintenanceEpisode", episode.Id, "Completed", actor, null);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task<long> AddMaintenanceTaskAsync(
        long episodeId, long? templateId, string? customTitleVi,
        CancellationToken cancellationToken = default)
    {
        var actor = RequireActor(a => a.IsAdmin || a.CanManageFleet);
        var vehicleId = await db.Set<MaintenanceEpisode>().AsNoTracking().Where(e => e.Id == episodeId)
            .Select(e => (long?)e.PhysicalVehicleId).SingleOrDefaultAsync(cancellationToken);
        if (vehicleId is null) throw new CatalogFleetRuleException("maintenance.not_open");
        return await InTransactionAsync(async () =>
        {
            await LockVehicleMaintenanceAsync(vehicleId.Value, cancellationToken);
            var episode = await db.Set<MaintenanceEpisode>().SingleOrDefaultAsync(
                e => e.Id == episodeId, cancellationToken);
            if (episode is null || episode.StatusCode is "Completed" or "Cancelled")
                throw new CatalogFleetRuleException("maintenance.not_open");
            await db.Entry(episode).ReloadAsync(cancellationToken);
            if (episode.StatusCode is "Completed" or "Cancelled")
                throw new CatalogFleetRuleException("maintenance.not_open");
            string title;
            if (templateId is not null)
            {
                var template = await db.Set<MaintenanceTaskTemplate>().SingleOrDefaultAsync(
                    t => t.Id == templateId && t.IsActive, cancellationToken);
                if (template is null) throw new CatalogFleetRuleException("maintenance.template_unavailable");
                title = template.TitleVi;
            }
            else if (!string.IsNullOrWhiteSpace(customTitleVi)) title = customTitleVi.Trim();
            else throw new CatalogFleetRuleException("maintenance.task_title_required");

            var now = clock.GetUtcNow().UtcDateTime;
            var item = new MaintenanceChecklistItem
            {
                MaintenanceEpisodeId = episodeId,
                TaskTemplateId = templateId,
                TitleSnapshotVi = title,
                StatusCode = "Pending",
                CreatedAtUtc = now,
                CreatedByReference = actor.Reference
            };
            db.Set<MaintenanceChecklistItem>().Add(item);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("MaintenanceChecklistItem", item.Id, "Added", actor, null);
            await db.SaveChangesAsync(cancellationToken);
            return item.Id;
        }, cancellationToken);
    }

    public async Task SetMaintenanceTaskStatusAsync(
        long itemId, string status, string? note, string? skipReason,
        CancellationToken cancellationToken = default, byte[]? expectedVersion = null)
    {
        if (status is not ("Completed" or "Skipped"))
            throw new CatalogFleetRuleException("maintenance.invalid_task_status");
        var actor = RequireActor(a => a.IsAdmin ||
            (status == "Skipped" ? a.CanApproveFleet : a.CanManageFleet));
        var vehicleId = await (from item in db.Set<MaintenanceChecklistItem>().AsNoTracking()
                               join episode in db.Set<MaintenanceEpisode>() on item.MaintenanceEpisodeId equals episode.Id
                               where item.Id == itemId select (long?)episode.PhysicalVehicleId)
            .SingleOrDefaultAsync(cancellationToken);
        if (vehicleId is null) throw new CatalogFleetRuleException("maintenance.task_not_found");
        await InTransactionAsync(async () =>
        {
            await LockVehicleMaintenanceAsync(vehicleId.Value, cancellationToken);
            var item = await db.Set<MaintenanceChecklistItem>().SingleOrDefaultAsync(
                i => i.Id == itemId, cancellationToken);
            if (item is null) throw new CatalogFleetRuleException("maintenance.task_not_found");
            var episode = await db.Set<MaintenanceEpisode>().SingleAsync(
                e => e.Id == item.MaintenanceEpisodeId, cancellationToken);
            await db.Entry(episode).ReloadAsync(cancellationToken);
            await db.Entry(item).ReloadAsync(cancellationToken);
            CheckVersion(item.RowVersion, expectedVersion);
            if (episode.StatusCode is "Completed" or "Cancelled")
                throw new CatalogFleetRuleException("maintenance.not_open");
            var now = clock.GetUtcNow().UtcDateTime;
            item.StatusCode = status;
            item.Note = note;
            item.SkipReason = status == "Skipped" ? skipReason : null;
            item.ChangedAtUtc = now;
            item.ChangedByReference = actor.Reference;
            item.UpdatedAtUtc = now;
            item.UpdatedByReference = actor.Reference;
            AddAudit("MaintenanceChecklistItem", item.Id, status, actor, skipReason);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task PublishModelAsync(long modelId, CancellationToken cancellationToken = default)
    {
        var actor = RequireActor(a => a.IsAdmin || a.CanPublishCatalog);
        await InTransactionAsync(async () =>
        {
            var model = await db.Set<VehicleModel>().SingleOrDefaultAsync(m => m.Id == modelId, cancellationToken);
            if (model is null || model.LifecycleStatus != "Active")
                throw new CatalogFleetRuleException("catalog.model_unavailable");
            var categoryReady = await db.Set<VehicleCategory>().AnyAsync(c =>
                c.Id == model.VehicleCategoryId && c.LifecycleStatus == "Active"
                && c.PublicationStatus == "Published", cancellationToken);
            var categoryVietnameseReady = await db.Set<VehicleCategoryTranslation>().AnyAsync(t =>
                t.VehicleCategoryId == model.VehicleCategoryId && t.Locale == "vi"
                && t.TranslationStatus == "Reviewed", cancellationToken);
            var brandReady = await db.Set<VehicleBrand>().AnyAsync(b =>
                b.Id == model.VehicleBrandId && b.LifecycleStatus == "Active", cancellationToken);
            var vietnameseReady = await db.Set<VehicleModelTranslation>().AnyAsync(t =>
                t.VehicleModelId == modelId && t.Locale == "vi"
                && t.TranslationStatus == "Reviewed" && t.SourceRevision == model.ContentRevision,
                cancellationToken);
            var imageReady = await db.Set<VehicleModelImage>().AnyAsync(i =>
                i.VehicleModelId == modelId && i.IsVisible && i.ApprovalStatus == "Approved"
                && i.RetiredAtUtc == null && i.SourceReference != null && i.RightsReference != null
                && i.ApprovedAtUtc != null && i.ApprovedByReference != null, cancellationToken);
            if (!categoryReady || !categoryVietnameseReady || !brandReady || !vietnameseReady || !imageReady)
                throw new CatalogFleetRuleException("catalog.publication_incomplete");

            var spec = await db.Set<VehicleModelSpecification>().SingleOrDefaultAsync(
                s => s.VehicleModelId == modelId, cancellationToken);
            if (spec is not null && (spec.VerificationStatus != "Verified"
                || spec.HasAbs is not null && string.IsNullOrWhiteSpace(spec.HasAbsSourceReference)
                || spec.EngineDisplacementCc is not null && string.IsNullOrWhiteSpace(spec.EngineDisplacementSourceReference)
                || spec.SeatCount is not null && string.IsNullOrWhiteSpace(spec.SeatCountSourceReference)))
                throw new CatalogFleetRuleException("catalog.specification_unverified");

            var now = clock.GetUtcNow().UtcDateTime;
            model.PublicationStatus = "Published";
            model.PublishedAtUtc ??= now;
            model.PublishedByReference ??= actor.Reference;
            model.UpdatedAtUtc = now;
            model.UpdatedByReference = actor.Reference;
            AddAudit("VehicleModel", modelId, "Published", actor, null);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task DeleteDraftVehicleAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        var actor = RequireActor(a => a.IsAdmin);
        await InTransactionAsync(async () =>
        {
            await LockVehicleMaintenanceAsync(vehicleId, cancellationToken);
            var vehicle = await db.Set<PhysicalVehicle>().SingleOrDefaultAsync(v => v.Id == vehicleId, cancellationToken);
            if (vehicle is null || vehicle.RecordLifecycle != "Draft")
                throw new CatalogFleetRuleException("vehicle.not_deletable_draft");
            var referenced = await db.Set<VehicleRegistration>().AnyAsync(r => r.PhysicalVehicleId == vehicleId, cancellationToken)
                || await db.Set<VehicleOperationalStatusHistory>().AnyAsync(h => h.PhysicalVehicleId == vehicleId, cancellationToken)
                || await db.Set<VehicleInspection>().AnyAsync(i => i.PhysicalVehicleId == vehicleId, cancellationToken)
                || await db.Set<MaintenanceEpisode>().AnyAsync(e => e.PhysicalVehicleId == vehicleId, cancellationToken)
                || await db.Set<CatalogFleetAuditEvent>().AnyAsync(a =>
                    a.EntityType == "PhysicalVehicle" && a.EntityId == vehicleId, cancellationToken);
            if (referenced) throw new CatalogFleetRuleException("vehicle.has_history");
            db.Set<PhysicalVehicle>().Remove(vehicle);
            AddAudit("PhysicalVehicle", vehicleId, "DraftDeleted", actor, null);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task DeleteDraftModelAsync(long modelId, CancellationToken cancellationToken = default)
    {
        var actor = RequireActor(a => a.IsAdmin);
        await InTransactionAsync(async () =>
        {
            var model = await db.Set<VehicleModel>().SingleOrDefaultAsync(m => m.Id == modelId, cancellationToken);
            if (model is null || model.PublicationStatus != "Draft" || model.PublishedAtUtc is not null)
                throw new CatalogFleetRuleException("catalog.model_not_deletable_draft");
            var referenced = await db.Set<VehicleModelTranslation>().AnyAsync(t => t.VehicleModelId == modelId, cancellationToken)
                || await db.Set<VehicleModelSpecification>().AnyAsync(s => s.VehicleModelId == modelId, cancellationToken)
                || await db.Set<VehicleModelImage>().AnyAsync(i => i.VehicleModelId == modelId, cancellationToken)
                || await db.Set<PhysicalVehicle>().AnyAsync(v => v.VehicleModelId == modelId, cancellationToken)
                || await db.Set<CatalogFleetAuditEvent>().AnyAsync(a =>
                    a.EntityType == "VehicleModel" && a.EntityId == modelId, cancellationToken);
            if (referenced) throw new CatalogFleetRuleException("catalog.model_has_history");
            db.Set<VehicleModel>().Remove(model);
            AddAudit("VehicleModel", modelId, "DraftDeleted", actor, null);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    private CatalogFleetActor RequireActor(Func<CatalogFleetActor, bool> permitted)
    {
        var actor = actorSource.GetRequiredActor();
        if (actor is null || string.IsNullOrWhiteSpace(actor.Reference) || !permitted(actor))
            throw new UnauthorizedAccessException("Catalog/Fleet write permission denied.");
        return actor;
    }

    private static void CheckVersion(byte[] actual, byte[]? expected)
    {
        if (expected is not null && !actual.AsSpan().SequenceEqual(expected))
            throw new DbUpdateConcurrencyException("The resource has changed.");
    }

    private void AddAudit(string entityType, long entityId, string action, CatalogFleetActor actor, string? reason)
    {
        db.Set<CatalogFleetAuditEvent>().Add(new CatalogFleetAuditEvent
        {
            OccurredAtUtc = clock.GetUtcNow().UtcDateTime,
            ActorReference = actor.Reference,
            EntityType = entityType,
            EntityId = entityId,
            ActionCode = action,
            Reason = reason,
            CorrelationId = Guid.NewGuid()
        });
    }

    private void AddStatusHistory(long vehicleId, string previous, string current,
        string reason, CatalogFleetActor actor, DateTime now)
    {
        if (previous == current) return;
        db.Set<VehicleOperationalStatusHistory>().Add(new VehicleOperationalStatusHistory
        {
            PhysicalVehicleId = vehicleId,
            PreviousStatus = previous,
            NewStatus = current,
            OccurredAtUtc = now,
            Reason = reason,
            CorrelationId = Guid.NewGuid(),
            SourceCode = "CatalogFleet",
            CreatedAtUtc = now,
            CreatedByReference = actor.Reference
        });
    }

    private async Task LockVehicleMaintenanceAsync(long vehicleId, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("A transaction is required for maintenance locking.");
        command.CommandText = "DECLARE @result int; EXEC @result = sp_getapplock " +
            "@Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000; SELECT @result;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.Value = $"SangRental:Maintenance:Vehicle:{vehicleId}";
        command.Parameters.Add(parameter);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is not int code || code < 0)
            throw new CatalogFleetRuleException("maintenance.lock_unavailable");
    }

    private async Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await using var owned = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var result = await action();
        if (owned is not null) await owned.CommitAsync(cancellationToken);
        return result;
    }
}
