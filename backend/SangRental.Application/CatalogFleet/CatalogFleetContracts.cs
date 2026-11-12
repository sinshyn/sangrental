using System.ComponentModel.DataAnnotations;

namespace SangRental.Application.CatalogFleet;

public sealed record CreateCategoryRequest(
    [property: Required, RegularExpression("^[a-z0-9][a-z0-9_-]{0,63}$")] string Code,
    [property: Required, RegularExpression("^(Motorcycle|Car)$")] string VehicleFamilyCode,
    [property: Required, StringLength(160, MinimumLength = 1)] string NameVi);
public sealed record CreateBrandRequest(
    [property: Required, RegularExpression("^[a-z0-9][a-z0-9_-]{0,63}$")] string Code,
    [property: Required, StringLength(160, MinimumLength = 1)] string DisplayName);
public sealed record CreateModelRequest(
    [property: Range(1, long.MaxValue)] long CategoryId,
    [property: Range(1, long.MaxValue)] long BrandId,
    [property: Required, RegularExpression("^[a-z0-9][a-z0-9_-]{0,63}$")] string Code,
    [property: Required, StringLength(160, MinimumLength = 1)] string CommercialName);
public sealed record CreateVehicleRequest(
    [property: Range(1, long.MaxValue)] long ModelId,
    [property: Required, StringLength(64, MinimumLength = 1)] string AssetCode,
    [property: StringLength(100)] string? Vin,
    bool ConfirmInitialInspection,
    [property: StringLength(1000)] string? DuplicateVinReason);
public sealed record ChangeVehicleLifecycleRequest(
    [property: Required, StringLength(1000, MinimumLength = 1)] string Reason,
    bool ConfirmInspection = false);
public sealed record UpdateVehicleProfileRequest(
    [property: Range(1, long.MaxValue)] long ModelId,
    [property: StringLength(80)] string? ColorName,
    short? ManufacturingYear,
    [property: StringLength(100)] string? Vin,
    [property: Required, StringLength(1000)] string Reason,
    [property: StringLength(1000)] string? DuplicateVinReason);
public sealed record ChangeVehicleRegistrationRequest(
    [property: Required, RegularExpression("^[A-Z]{2}$")] string JurisdictionCode,
    [property: Required, StringLength(32)] string Plate,
    [property: Required, StringLength(1000)] string Reason);
public sealed record VehicleRegistrationSummary(long Id, string JurisdictionCode, string Plate,
    DateTime ValidFromUtc, DateTime? ValidToUtc, string? ClosureReason);
public sealed record VehicleDetails(long Id, long ModelId, string AssetCode, string? Vin,
    string RecordLifecycle, string OperationalStatus, DateTime? ReadyForRentAtUtc,
    DateTime? RetiredAtUtc, string? RetirementReason, string RowVersion,
    string? ColorName, short? ManufacturingYear);
public sealed record ScheduleMaintenanceRequest(
    DateTime ScheduledStartUtc, DateTime? ScheduledEndUtc,
    [property: Required, StringLength(1000, MinimumLength = 1)] string Reason);
public sealed record AddMaintenanceTaskRequest(long? TemplateId,
    [property: StringLength(200)] string? CustomTitleVi);
public sealed record SetMaintenanceTaskRequest(
    [property: Required, RegularExpression("^(Completed|Skipped)$")] string Status,
    [property: StringLength(2000)] string? Note, [property: StringLength(1000)] string? SkipReason);
public sealed record CategorySummary(long Id, string Code, string VehicleFamilyCode, string PublicationStatus);
public sealed record BrandSummary(long Id, string Code, string DisplayName);
public sealed record ModelSummary(long Id, string Code, string CommercialName, string PublicationStatus);
public sealed record VehicleSummary(long Id, long ModelId, string AssetCode, string RecordLifecycle, string OperationalStatus, string RowVersion);
public sealed record MaintenanceSummary(long Id, long VehicleId, DateTime ScheduledStartUtc, DateTime? ScheduledEndUtc,
    string Status, DateTime? ActualEndUtc, string RowVersion);
public sealed record MaintenanceTaskSummary(long Id, string TitleVi, string Status, string RowVersion);
