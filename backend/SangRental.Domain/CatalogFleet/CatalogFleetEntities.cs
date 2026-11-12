namespace SangRental.Domain.CatalogFleet;

// Catalog and fleet records contain no booking, payment, or identity behavior.
public abstract class CatalogFleetRecord
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByReference { get; set; } = null!;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedByReference { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class VehicleCategory : CatalogFleetRecord
{
    public string Code { get; set; } = null!;
    public string VehicleFamilyCode { get; set; } = null!;
    public string LifecycleStatus { get; set; } = "Active";
    public string PublicationStatus { get; set; } = "Draft";
    public int DisplayOrder { get; set; }
    public int ContentRevision { get; set; } = 1;
    public DateTime? RetiredAtUtc { get; set; }
    public string? RetirementReason { get; set; }
}

public sealed class VehicleCategoryTranslation
{
    public long VehicleCategoryId { get; set; }
    public string Locale { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Slug { get; set; } = null!;
    public string TranslationStatus { get; set; } = "Draft";
    public int SourceRevision { get; set; } = 1;
    public string? ReviewedByReference { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class VehicleBrand : CatalogFleetRecord
{
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string LifecycleStatus { get; set; } = "Active";
    public DateTime? RetiredAtUtc { get; set; }
    public string? RetirementReason { get; set; }
}

public sealed class VehicleModel : CatalogFleetRecord
{
    public long VehicleCategoryId { get; set; }
    public long VehicleBrandId { get; set; }
    public string Code { get; set; } = null!;
    public string CommercialName { get; set; } = null!;
    public string? EditionLabel { get; set; }
    public short? ModelYear { get; set; }
    public int ContentRevision { get; set; } = 1;
    public string LifecycleStatus { get; set; } = "Active";
    public string PublicationStatus { get; set; } = "Draft";
    public DateTime? PublishedAtUtc { get; set; }
    public string? PublishedByReference { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? RetiredAtUtc { get; set; }
    public string? RetirementReason { get; set; }
}

public sealed class VehicleModelTranslation
{
    public long VehicleModelId { get; set; }
    public string Locale { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string Slug { get; set; } = null!;
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string TranslationStatus { get; set; } = "Draft";
    public int SourceRevision { get; set; } = 1;
    public string? ReviewedByReference { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class VehicleModelSpecification
{
    public long VehicleModelId { get; set; }
    public byte? SeatCount { get; set; }
    public decimal? EngineDisplacementCc { get; set; }
    public decimal? FuelConsumptionLitresPer100Km { get; set; }
    public decimal? LuggageCapacityLitres { get; set; }
    public decimal? WeightKg { get; set; }
    public string? TransmissionCode { get; set; }
    public string? EnergyTypeCode { get; set; }
    public bool? HasAbs { get; set; }
    public string? SeatCountSourceReference { get; set; }
    public string? EngineDisplacementSourceReference { get; set; }
    public string? HasAbsSourceReference { get; set; }
    public string VerificationStatus { get; set; } = "Draft";
    public DateTime? VerifiedAtUtc { get; set; }
    public string? VerifiedByReference { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByReference { get; set; } = null!;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedByReference { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class VehicleModelImage : CatalogFleetRecord
{
    public long VehicleModelId { get; set; }
    public string StorageKey { get; set; } = null!;
    public byte[] ContentHash { get; set; } = null!;
    public string MediaType { get; set; } = null!;
    public int Width { get; set; }
    public int Height { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsVisible { get; set; }
    public string ApprovalStatus { get; set; } = "Draft";
    public string? SourceReference { get; set; }
    public string? RightsReference { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedByReference { get; set; }
    public DateTime? RetiredAtUtc { get; set; }
}

public sealed class VehicleModelImageTranslation
{
    public long VehicleModelImageId { get; set; }
    public string Locale { get; set; } = null!;
    public string AltText { get; set; } = null!;
    public string? Caption { get; set; }
    public string TranslationStatus { get; set; } = "Draft";
    public int SourceRevision { get; set; } = 1;
    public string? ReviewedByReference { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class PhysicalVehicle : CatalogFleetRecord
{
    public long VehicleModelId { get; set; }
    public string AssetCode { get; set; } = null!;
    public short? ManufacturingYear { get; set; }
    public string? ColorName { get; set; }
    public string? VinOrChassisNumber { get; set; }
    public string? NormalizedVinOrChassisNumber { get; set; }
    public string RecordLifecycle { get; set; } = "Draft";
    public string OperationalStatus { get; set; } = "PendingInspection";
    public DateTime? LastReceivedAtUtc { get; set; }
    public DateTime? ReadyForRentAtUtc { get; set; }
    public DateTime? RetiredAtUtc { get; set; }
    public string? RetirementReason { get; set; }
}

public sealed class VehicleRegistration
{
    public long Id { get; set; }
    public long PhysicalVehicleId { get; set; }
    public string RegistrationJurisdictionCode { get; set; } = null!;
    public string PlateDisplay { get; set; } = null!;
    public string NormalizedPlate { get; set; } = null!;
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedByReference { get; set; }
    public string? ClosureReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByReference { get; set; } = null!;
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class VehicleOperationalStatusHistory
{
    public long Id { get; set; }
    public long PhysicalVehicleId { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = null!;
    public DateTime OccurredAtUtc { get; set; }
    public string Reason { get; set; } = null!;
    public Guid CorrelationId { get; set; }
    public string? ExternalEventId { get; set; }
    public string SourceCode { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByReference { get; set; } = null!;
}

public sealed class VehicleInspection
{
    public long Id { get; set; }
    public long PhysicalVehicleId { get; set; }
    public string InspectionTypeCode { get; set; } = null!;
    public string ResultCode { get; set; } = null!;
    public DateTime InspectedAtUtc { get; set; }
    public string InspectorReference { get; set; } = null!;
    public decimal? OdometerKm { get; set; }
    public string? Findings { get; set; }
    public string? EvidenceReference { get; set; }
    public string? SourceReference { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByReference { get; set; } = null!;
}

public sealed class MaintenanceEpisode : CatalogFleetRecord
{
    public long PhysicalVehicleId { get; set; }
    public DateTime ScheduledStartUtc { get; set; }
    public DateTime? ScheduledEndUtc { get; set; }
    public DateTime? ActualStartUtc { get; set; }
    public DateTime? ActualEndUtc { get; set; }
    public string StatusCode { get; set; } = "Planned";
    public string Reason { get; set; } = null!;
    public DateTime? ReleasedAtUtc { get; set; }
    public string? ReleasedByReference { get; set; }
    public long? ReleaseInspectionId { get; set; }
    public string? CancellationReason { get; set; }
}

public sealed class MaintenanceTaskTemplate : CatalogFleetRecord
{
    public string Code { get; set; } = null!;
    public string TitleVi { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public sealed class MaintenanceChecklistItem : CatalogFleetRecord
{
    public long MaintenanceEpisodeId { get; set; }
    public long? TaskTemplateId { get; set; }
    public string TitleSnapshotVi { get; set; } = null!;
    public string StatusCode { get; set; } = "Pending";
    public string? Note { get; set; }
    public string? SkipReason { get; set; }
    public DateTime? ChangedAtUtc { get; set; }
    public string? ChangedByReference { get; set; }
}

public sealed class CatalogFleetAuditEvent
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string ActorReference { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public long EntityId { get; set; }
    public string ActionCode { get; set; } = null!;
    public string? Reason { get; set; }
    public Guid CorrelationId { get; set; }
}
