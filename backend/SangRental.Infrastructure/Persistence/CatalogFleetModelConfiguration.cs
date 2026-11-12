using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SangRental.Domain.CatalogFleet;

namespace SangRental.Infrastructure.Persistence;

internal static class CatalogFleetModelConfiguration
{
    private const string Locales = "[Locale] IN ('vi','en','ko','zh-CN','ja','th')";

    public static void Configure(ModelBuilder modelBuilder)
    {
        ConfigureCategory(modelBuilder.Entity<VehicleCategory>());
        ConfigureCategoryTranslation(modelBuilder.Entity<VehicleCategoryTranslation>());
        ConfigureBrand(modelBuilder.Entity<VehicleBrand>());
        ConfigureModel(modelBuilder.Entity<VehicleModel>());
        ConfigureModelTranslation(modelBuilder.Entity<VehicleModelTranslation>());
        ConfigureSpecification(modelBuilder.Entity<VehicleModelSpecification>());
        ConfigureImage(modelBuilder.Entity<VehicleModelImage>());
        ConfigureImageTranslation(modelBuilder.Entity<VehicleModelImageTranslation>());
        ConfigureVehicle(modelBuilder.Entity<PhysicalVehicle>());
        ConfigureRegistration(modelBuilder.Entity<VehicleRegistration>());
        ConfigureStatusHistory(modelBuilder.Entity<VehicleOperationalStatusHistory>());
        ConfigureInspection(modelBuilder.Entity<VehicleInspection>());
        ConfigureEpisode(modelBuilder.Entity<MaintenanceEpisode>());
        ConfigureTaskTemplate(modelBuilder.Entity<MaintenanceTaskTemplate>());
        ConfigureChecklistItem(modelBuilder.Entity<MaintenanceChecklistItem>());
        ConfigureAuditEvent(modelBuilder.Entity<CatalogFleetAuditEvent>());
    }

    private static void Record<T>(EntityTypeBuilder<T> e) where T : CatalogFleetRecord
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedOnAdd();
        e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.CreatedByReference).HasMaxLength(128).IsRequired();
        e.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(7)");
        e.Property(x => x.UpdatedByReference).HasMaxLength(128);
        e.Property(x => x.RowVersion).IsRowVersion();
    }

    private static void Locale<T>(EntityTypeBuilder<T> e) where T : class
    {
        e.Property<string>("Locale").HasMaxLength(5).IsUnicode(false)
            .UseCollation("Latin1_General_100_BIN2");
        e.ToTable(t => t.HasCheckConstraint($"CK_{typeof(T).Name}_Locale", Locales));
        e.HasIndex("Locale", "Slug").IsUnique();
    }

    private static void Code<T>(EntityTypeBuilder<T> e, string propertyName = "Code") where T : class
    {
        e.Property<string>(propertyName).HasMaxLength(64).IsUnicode(false)
            .UseCollation("Latin1_General_100_BIN2");
        e.HasIndex(propertyName).IsUnique();
        if (propertyName == "Code")
        {
            e.ToTable(t => t.HasCheckConstraint(
                $"CK_{typeof(T).Name}_CodeCanonical",
                "DATALENGTH([Code]) > 0 AND [Code] = LOWER([Code]) " +
                "AND CHARINDEX(' ', [Code]) = 0 " +
                "AND CHARINDEX(CHAR(9), [Code]) = 0 " +
                "AND CHARINDEX(CHAR(10), [Code]) = 0 " +
                "AND CHARINDEX(CHAR(11), [Code]) = 0 " +
                "AND CHARINDEX(CHAR(12), [Code]) = 0 " +
                "AND CHARINDEX(CHAR(13), [Code]) = 0"));
        }
    }

    private static void ConfigureCategory(EntityTypeBuilder<VehicleCategory> e)
    {
        e.ToTable("VehicleCategories", t =>
        {
            t.HasCheckConstraint("CK_VehicleCategories_Family", "[VehicleFamilyCode] IN ('Motorcycle','Car')");
            t.HasCheckConstraint("CK_VehicleCategories_Lifecycle", "[LifecycleStatus] IN ('Active','Retired')");
            t.HasCheckConstraint("CK_VehicleCategories_Publication", "[PublicationStatus] IN ('Draft','Published','Hidden')");
            t.HasCheckConstraint("CK_VehicleCategories_Order", "[DisplayOrder] >= 0");
            t.HasCheckConstraint("CK_VehicleCategories_Revision", "[ContentRevision] > 0");
        });
        Record(e); Code(e);
        e.Property(x => x.VehicleFamilyCode).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.LifecycleStatus).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.PublicationStatus).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.RetirementReason).HasMaxLength(1000);
    }

    private static void ConfigureCategoryTranslation(EntityTypeBuilder<VehicleCategoryTranslation> e)
    {
        e.ToTable("VehicleCategoryTranslations");
        e.HasKey(x => new { x.VehicleCategoryId, x.Locale });
        e.HasOne<VehicleCategory>().WithMany().HasForeignKey(x => x.VehicleCategoryId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.Name).HasMaxLength(160).IsRequired();
        e.Property(x => x.Description).HasMaxLength(2000);
        e.Property(x => x.Slug).HasMaxLength(160).IsUnicode(false).IsRequired();
        e.Property(x => x.TranslationStatus).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.ReviewedByReference).HasMaxLength(128);
        e.Property(x => x.RowVersion).IsRowVersion();
        Locale(e);
    }

    private static void ConfigureBrand(EntityTypeBuilder<VehicleBrand> e)
    {
        e.ToTable("VehicleBrands", t => t.HasCheckConstraint("CK_VehicleBrands_Lifecycle", "[LifecycleStatus] IN ('Active','Retired')"));
        Record(e); Code(e);
        e.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        e.Property(x => x.LifecycleStatus).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.RetirementReason).HasMaxLength(1000);
    }

    private static void ConfigureModel(EntityTypeBuilder<VehicleModel> e)
    {
        e.ToTable("VehicleModels", t =>
        {
            t.HasCheckConstraint("CK_VehicleModels_Publication", "[PublicationStatus] IN ('Draft','Published','Hidden')");
            t.HasCheckConstraint("CK_VehicleModels_Lifecycle", "[LifecycleStatus] IN ('Active','Retired')");
            t.HasCheckConstraint("CK_VehicleModels_Revision", "[ContentRevision] > 0");
            t.HasCheckConstraint("CK_VehicleModels_Order", "[DisplayOrder] >= 0");
        });
        Record(e); Code(e);
        e.HasOne<VehicleCategory>().WithMany().HasForeignKey(x => x.VehicleCategoryId).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<VehicleBrand>().WithMany().HasForeignKey(x => x.VehicleBrandId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.CommercialName).HasMaxLength(160).IsRequired();
        e.Property(x => x.EditionLabel).HasMaxLength(80);
        e.Property(x => x.LifecycleStatus).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.PublicationStatus).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.PublishedByReference).HasMaxLength(128);
        e.Property(x => x.RetirementReason).HasMaxLength(1000);
        e.HasIndex(x => new { x.VehicleCategoryId, x.PublicationStatus });
    }

    private static void ConfigureModelTranslation(EntityTypeBuilder<VehicleModelTranslation> e)
    {
        e.ToTable("VehicleModelTranslations");
        e.HasKey(x => new { x.VehicleModelId, x.Locale });
        e.HasOne<VehicleModel>().WithMany().HasForeignKey(x => x.VehicleModelId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        e.Property(x => x.Summary).HasMaxLength(1000);
        e.Property(x => x.Description).HasColumnType("nvarchar(max)");
        e.Property(x => x.Slug).HasMaxLength(160).IsUnicode(false).IsRequired();
        e.Property(x => x.SeoTitle).HasMaxLength(200);
        e.Property(x => x.SeoDescription).HasMaxLength(500);
        e.Property(x => x.TranslationStatus).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.ReviewedByReference).HasMaxLength(128);
        e.Property(x => x.RowVersion).IsRowVersion();
        Locale(e);
    }

    private static void ConfigureSpecification(EntityTypeBuilder<VehicleModelSpecification> e)
    {
        e.ToTable("VehicleModelSpecifications", t =>
        {
            t.HasCheckConstraint("CK_Specifications_SeatCount", "[SeatCount] IS NULL OR [SeatCount] > 0");
            t.HasCheckConstraint("CK_Specifications_Engine", "[EngineDisplacementCc] IS NULL OR [EngineDisplacementCc] > 0");
            t.HasCheckConstraint("CK_Specifications_Fuel", "[FuelConsumptionLitresPer100Km] IS NULL OR [FuelConsumptionLitresPer100Km] > 0");
        });
        e.HasKey(x => x.VehicleModelId);
        e.HasOne<VehicleModel>().WithOne().HasForeignKey<VehicleModelSpecification>(x => x.VehicleModelId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.EngineDisplacementCc).HasPrecision(7, 2);
        e.Property(x => x.FuelConsumptionLitresPer100Km).HasPrecision(6, 3);
        e.Property(x => x.LuggageCapacityLitres).HasPrecision(8, 2);
        e.Property(x => x.WeightKg).HasPrecision(8, 2);
        e.Property(x => x.TransmissionCode).HasMaxLength(24).IsUnicode(false);
        e.Property(x => x.EnergyTypeCode).HasMaxLength(24).IsUnicode(false);
        e.Property(x => x.SeatCountSourceReference).HasMaxLength(500);
        e.Property(x => x.EngineDisplacementSourceReference).HasMaxLength(500);
        e.Property(x => x.HasAbsSourceReference).HasMaxLength(500);
        e.Property(x => x.VerificationStatus).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.VerifiedByReference).HasMaxLength(128);
        e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.CreatedByReference).HasMaxLength(128).IsRequired();
        e.Property(x => x.UpdatedByReference).HasMaxLength(128);
        e.Property(x => x.RowVersion).IsRowVersion();
    }

    private static void ConfigureImage(EntityTypeBuilder<VehicleModelImage> e)
    {
        e.ToTable("VehicleModelImages", t =>
        {
            t.HasCheckConstraint("CK_VehicleModelImages_Size", "[Width] > 0 AND [Height] > 0");
            t.HasCheckConstraint("CK_VehicleModelImages_Order", "[DisplayOrder] >= 0");
            t.HasCheckConstraint("CK_VehicleModelImages_Approval", "[ApprovalStatus] IN ('Draft','Approved','Rejected')");
        });
        Record(e);
        e.HasOne<VehicleModel>().WithMany().HasForeignKey(x => x.VehicleModelId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.StorageKey).HasMaxLength(450).IsRequired();
        e.Property(x => x.ContentHash).HasColumnType("binary(32)").IsRequired();
        e.Property(x => x.MediaType).HasMaxLength(64).IsUnicode(false);
        e.Property(x => x.ApprovalStatus).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.SourceReference).HasMaxLength(500);
        e.Property(x => x.RightsReference).HasMaxLength(500);
        e.Property(x => x.ApprovedByReference).HasMaxLength(128);
        e.HasIndex(x => x.VehicleModelId).HasFilter("[IsPrimary] = 1").IsUnique();
    }

    private static void ConfigureImageTranslation(EntityTypeBuilder<VehicleModelImageTranslation> e)
    {
        e.ToTable("VehicleModelImageTranslations", t => t.HasCheckConstraint("CK_ImageTranslation_Locale", Locales));
        e.HasKey(x => new { x.VehicleModelImageId, x.Locale });
        e.HasOne<VehicleModelImage>().WithMany().HasForeignKey(x => x.VehicleModelImageId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.Locale).HasMaxLength(5).IsUnicode(false).UseCollation("Latin1_General_100_BIN2");
        e.Property(x => x.AltText).HasMaxLength(300).IsRequired();
        e.Property(x => x.Caption).HasMaxLength(1000);
        e.Property(x => x.TranslationStatus).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.ReviewedByReference).HasMaxLength(128);
        e.Property(x => x.RowVersion).IsRowVersion();
    }

    private static void ConfigureVehicle(EntityTypeBuilder<PhysicalVehicle> e)
    {
        e.ToTable("PhysicalVehicles", t =>
        {
            t.HasCheckConstraint("CK_PhysicalVehicles_RecordLifecycle", "[RecordLifecycle] IN ('Draft','Active','Retired')");
            t.HasCheckConstraint("CK_PhysicalVehicles_OperationalStatus", "[OperationalStatus] IN ('PendingInspection','Ready','ReturnInspection','Maintenance','Suspended','LostLocked','AccidentLocked')");
            t.HasCheckConstraint("CK_PhysicalVehicles_VinPair", "([VinOrChassisNumber] IS NULL AND [NormalizedVinOrChassisNumber] IS NULL) OR ([VinOrChassisNumber] IS NOT NULL AND [NormalizedVinOrChassisNumber] IS NOT NULL)");
        });
        Record(e);
        e.HasOne<VehicleModel>().WithMany().HasForeignKey(x => x.VehicleModelId).OnDelete(DeleteBehavior.NoAction);
        Code(e, nameof(PhysicalVehicle.AssetCode));
        e.Property(x => x.ColorName).HasMaxLength(80);
        e.Property(x => x.VinOrChassisNumber).HasMaxLength(100);
        e.Property(x => x.NormalizedVinOrChassisNumber).HasMaxLength(100).IsUnicode(false);
        e.Property(x => x.RecordLifecycle).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.OperationalStatus).HasMaxLength(32).IsUnicode(false);
        e.Property(x => x.RetirementReason).HasMaxLength(1000);
        e.HasIndex(x => x.NormalizedVinOrChassisNumber).HasFilter("[NormalizedVinOrChassisNumber] IS NOT NULL");
        e.HasIndex(x => new { x.VehicleModelId, x.RecordLifecycle, x.OperationalStatus });
    }

    private static void ConfigureRegistration(EntityTypeBuilder<VehicleRegistration> e)
    {
        e.ToTable("VehicleRegistrations", t => t.HasCheckConstraint("CK_VehicleRegistrations_Interval", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]"));
        e.HasKey(x => x.Id);
        e.HasOne<PhysicalVehicle>().WithMany().HasForeignKey(x => x.PhysicalVehicleId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.RegistrationJurisdictionCode).HasMaxLength(32).IsUnicode(false);
        e.Property(x => x.PlateDisplay).HasMaxLength(32).IsRequired();
        e.Property(x => x.NormalizedPlate).HasMaxLength(32).IsUnicode(false);
        e.Property(x => x.ClosedByReference).HasMaxLength(128);
        e.Property(x => x.ClosureReason).HasMaxLength(1000);
        e.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.CreatedByReference).HasMaxLength(128);
        e.Property(x => x.RowVersion).IsRowVersion();
        e.HasIndex(x => x.PhysicalVehicleId).HasFilter("[ValidToUtc] IS NULL").IsUnique();
        e.HasIndex(x => new { x.RegistrationJurisdictionCode, x.NormalizedPlate })
            .HasFilter("[ValidToUtc] IS NULL").IsUnique();
    }

    private static void ConfigureStatusHistory(EntityTypeBuilder<VehicleOperationalStatusHistory> e)
    {
        e.ToTable("VehicleOperationalStatusHistories");
        e.HasKey(x => x.Id);
        e.HasOne<PhysicalVehicle>().WithMany().HasForeignKey(x => x.PhysicalVehicleId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.PreviousStatus).HasMaxLength(32).IsUnicode(false);
        e.Property(x => x.NewStatus).HasMaxLength(32).IsUnicode(false);
        e.Property(x => x.Reason).HasMaxLength(1000);
        e.Property(x => x.ExternalEventId).HasMaxLength(128).IsUnicode(false);
        e.Property(x => x.SourceCode).HasMaxLength(32).IsUnicode(false);
        e.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.CreatedByReference).HasMaxLength(128);
        e.HasIndex(x => new { x.SourceCode, x.ExternalEventId }).HasFilter("[ExternalEventId] IS NOT NULL").IsUnique();
    }

    private static void ConfigureInspection(EntityTypeBuilder<VehicleInspection> e)
    {
        e.ToTable("VehicleInspections", t => t.HasCheckConstraint("CK_VehicleInspections_Result", "[ResultCode] IN ('Passed','Failed','NeedsReview')"));
        e.HasKey(x => x.Id);
        e.HasOne<PhysicalVehicle>().WithMany().HasForeignKey(x => x.PhysicalVehicleId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.InspectionTypeCode).HasMaxLength(32).IsUnicode(false);
        e.Property(x => x.ResultCode).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.InspectorReference).HasMaxLength(128);
        e.Property(x => x.OdometerKm).HasPrecision(12, 1);
        e.Property(x => x.Findings).HasMaxLength(2000);
        e.Property(x => x.EvidenceReference).HasMaxLength(450);
        e.Property(x => x.SourceReference).HasMaxLength(500);
        e.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.CreatedByReference).HasMaxLength(128);
        e.HasIndex(x => new { x.PhysicalVehicleId, x.InspectedAtUtc });
    }

    private static void ConfigureEpisode(EntityTypeBuilder<MaintenanceEpisode> e)
    {
        e.ToTable("MaintenanceEpisodes", t =>
        {
            t.HasCheckConstraint("CK_MaintenanceEpisodes_PlannedInterval", "[ScheduledEndUtc] IS NULL OR [ScheduledEndUtc] > [ScheduledStartUtc]");
            t.HasCheckConstraint("CK_MaintenanceEpisodes_ActualInterval", "[ActualEndUtc] IS NULL OR ([ActualStartUtc] IS NOT NULL AND [ActualEndUtc] >= [ActualStartUtc])");
            t.HasCheckConstraint("CK_MaintenanceEpisodes_Status", "[StatusCode] IN ('Planned','InProgress','Completed','Cancelled')");
        });
        Record(e);
        e.HasOne<PhysicalVehicle>().WithMany().HasForeignKey(x => x.PhysicalVehicleId).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<VehicleInspection>().WithMany().HasForeignKey(x => x.ReleaseInspectionId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.StatusCode).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.Reason).HasMaxLength(1000);
        e.Property(x => x.ReleasedByReference).HasMaxLength(128);
        e.Property(x => x.CancellationReason).HasMaxLength(1000);
        e.HasIndex(x => new { x.PhysicalVehicleId, x.ScheduledStartUtc });
    }

    private static void ConfigureTaskTemplate(EntityTypeBuilder<MaintenanceTaskTemplate> e)
    {
        e.ToTable("MaintenanceTaskTemplates");
        Record(e); Code(e);
        e.Property(x => x.TitleVi).HasMaxLength(200).IsRequired();
    }

    private static void ConfigureChecklistItem(EntityTypeBuilder<MaintenanceChecklistItem> e)
    {
        e.ToTable("MaintenanceChecklistItems", t => t.HasCheckConstraint("CK_MaintenanceChecklistItems_Status", "[StatusCode] IN ('Pending','Completed','Skipped')"));
        Record(e);
        e.HasOne<MaintenanceEpisode>().WithMany().HasForeignKey(x => x.MaintenanceEpisodeId).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<MaintenanceTaskTemplate>().WithMany().HasForeignKey(x => x.TaskTemplateId).OnDelete(DeleteBehavior.NoAction);
        e.Property(x => x.TitleSnapshotVi).HasMaxLength(200).IsRequired();
        e.Property(x => x.StatusCode).HasMaxLength(16).IsUnicode(false);
        e.Property(x => x.Note).HasMaxLength(2000);
        e.Property(x => x.SkipReason).HasMaxLength(1000);
        e.Property(x => x.ChangedByReference).HasMaxLength(128);
        e.HasIndex(x => new { x.MaintenanceEpisodeId, x.Id });
    }

    private static void ConfigureAuditEvent(EntityTypeBuilder<CatalogFleetAuditEvent> e)
    {
        e.ToTable("CatalogFleetAuditEvents");
        e.HasKey(x => x.Id);
        e.Property(x => x.OccurredAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.ActorReference).HasMaxLength(128).IsRequired();
        e.Property(x => x.EntityType).HasMaxLength(64).IsUnicode(false);
        e.Property(x => x.ActionCode).HasMaxLength(64).IsUnicode(false);
        e.Property(x => x.Reason).HasMaxLength(1000);
        e.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAtUtc });
    }
}
