using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SangRental.Application.CatalogFleet;
using SangRental.Application.Media;
using SangRental.Domain.CatalogFleet;

namespace SangRental.Infrastructure.Persistence;

public sealed class CatalogImageService(SangRentalDbContext db, ICatalogFleetActorSource actors,
    IImageProcessingService processor, IMediaStorage storage, TimeProvider clock, ILogger<CatalogImageService> logger)
{
    public async Task<long> UploadAsync(long modelId, Stream file, string originalName, CatalogImageUpload request, CancellationToken ct)
    {
        var actor = Admin();
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (!await db.Set<VehicleModel>().AnyAsync(x => x.Id == modelId && x.LifecycleStatus == "Active", ct))
            throw new MediaValidationException("media.model_unavailable");
        var prepared = await processor.PrepareAsync(file, originalName, MediaPurpose.Catalog, ct);
        var saved = await storage.SaveNewAsync(prepared, ct);
        var committed = false;
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            await SqlApplicationLock.AcquireAsync(db, $"SangRental:CatalogImages:{modelId}", ct);
            if (!await db.Set<VehicleModel>().AnyAsync(x => x.Id == modelId && x.LifecycleStatus == "Active", ct))
                throw new MediaValidationException("media.model_unavailable");
            var image = new VehicleModelImage
            {
                VehicleModelId = modelId, StorageKey = saved.StorageKey, ContentHash = saved.ContentHash,
                MediaType = saved.MediaType, Width = saved.Width, Height = saved.Height,
                SourceReference = request.SourceReference.Trim(), RightsReference = request.RightsReference.Trim(),
                CreatedAtUtc = clock.GetUtcNow().UtcDateTime, CreatedByReference = actor.Reference
            };
            db.Add(image);
            await db.SaveChangesAsync(ct);
            db.Add(new VehicleModelImageTranslation { VehicleModelImageId = image.Id, Locale = "vi", AltText = request.AltTextVi.Trim() });
            Audit(image.Id, "Uploaded", actor.Reference);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            committed = true;
            return image.Id;
        }
        catch
        {
            // A commit error may be ambiguous. Only delete after a separate successful DB check proves no reference.
            if (!committed)
            {
                try
                {
                    var referenced = await db.Set<VehicleModelImage>().AsNoTracking().AnyAsync(x => x.StorageKey == saved.StorageKey, CancellationToken.None);
                    if (!referenced) storage.DeleteUncommitted(saved);
                }
                catch { logger.LogWarning("Uncommitted media cleanup requires reconciliation; no file details logged."); }
            }
            throw;
        }
    }

    public async Task ChangeAsync(long modelId, long imageId, string action, byte[] version, CancellationToken ct)
    {
        var actor = Admin();
        if (version.Length != 8) throw new ValidationException("media.invalid_version");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await SqlApplicationLock.AcquireAsync(db, $"SangRental:CatalogImages:{modelId}", ct);
        var image = await db.Set<VehicleModelImage>().SingleOrDefaultAsync(x => x.Id == imageId && x.VehicleModelId == modelId, ct)
            ?? throw new MediaValidationException("media.not_found");
        if (!image.RowVersion.AsSpan().SequenceEqual(version)) throw new DbUpdateConcurrencyException();
        if (image.RetiredAtUtc != null) throw new MediaValidationException("media.retired");
        var now = clock.GetUtcNow().UtcDateTime;
        switch (action)
        {
            case "approve":
                if (string.IsNullOrWhiteSpace(image.SourceReference) || string.IsNullOrWhiteSpace(image.RightsReference))
                    throw new MediaValidationException("media.rights_required");
                if (image.ApprovalStatus == "Approved" && image.IsVisible) throw new MediaValidationException("media.unchanged");
                image.ApprovalStatus = "Approved"; image.ApprovedAtUtc = now; image.ApprovedByReference = actor.Reference; image.IsVisible = true;
                break;
            case "primary":
                if (!image.IsVisible || image.ApprovalStatus != "Approved") throw new MediaValidationException("media.not_approved");
                if (image.IsPrimary) throw new MediaValidationException("media.unchanged");
                var previous = await db.Set<VehicleModelImage>().SingleOrDefaultAsync(x => x.VehicleModelId == modelId && x.IsPrimary, ct);
                if (previous is not null)
                {
                    previous.IsPrimary = false; previous.UpdatedAtUtc = now; previous.UpdatedByReference = actor.Reference;
                    await db.SaveChangesAsync(ct); // Release the filtered unique index before setting new primary.
                }
                image.IsPrimary = true;
                break;
            case "hide": image.IsVisible = false; image.IsPrimary = false; break;
            case "retire": image.IsVisible = false; image.IsPrimary = false; image.RetiredAtUtc = now; break;
            default: throw new ValidationException("media.invalid_action");
        }
        image.UpdatedAtUtc = now; image.UpdatedByReference = actor.Reference;
        Audit(imageId, action, actor.Reference);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private CatalogFleetActor Admin()
    {
        var actor = actors.GetRequiredActor();
        if (!actor.IsAdmin) throw new UnauthorizedAccessException();
        return actor;
    }
    private void Audit(long id, string action, string actor) => db.Add(new CatalogFleetAuditEvent
    {
        EntityType = "VehicleModelImage", EntityId = id, ActionCode = action,
        ActorReference = actor, OccurredAtUtc = clock.GetUtcNow().UtcDateTime
    });
}
