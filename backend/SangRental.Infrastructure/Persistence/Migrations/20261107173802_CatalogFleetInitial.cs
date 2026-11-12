using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SangRental.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogFleetInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sangrental");

            migrationBuilder.CreateTable(
                name: "CatalogFleetAuditEvents",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ActorReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    EntityId = table.Column<long>(type: "bigint", nullable: false),
                    ActionCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogFleetAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTaskTemplates",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    TitleVi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTaskTemplates", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceTaskTemplate_CodeCanonical", "DATALENGTH([Code]) > 0 AND [Code] = LOWER([Code]) AND CHARINDEX(' ', [Code]) = 0 AND CHARINDEX(CHAR(9), [Code]) = 0 AND CHARINDEX(CHAR(10), [Code]) = 0 AND CHARINDEX(CHAR(11), [Code]) = 0 AND CHARINDEX(CHAR(12), [Code]) = 0 AND CHARINDEX(CHAR(13), [Code]) = 0");
                });

            migrationBuilder.CreateTable(
                name: "VehicleBrands",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    LifecycleStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirementReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleBrands", x => x.Id);
                    table.CheckConstraint("CK_VehicleBrand_CodeCanonical", "DATALENGTH([Code]) > 0 AND [Code] = LOWER([Code]) AND CHARINDEX(' ', [Code]) = 0 AND CHARINDEX(CHAR(9), [Code]) = 0 AND CHARINDEX(CHAR(10), [Code]) = 0 AND CHARINDEX(CHAR(11), [Code]) = 0 AND CHARINDEX(CHAR(12), [Code]) = 0 AND CHARINDEX(CHAR(13), [Code]) = 0");
                    table.CheckConstraint("CK_VehicleBrands_Lifecycle", "[LifecycleStatus] IN ('Active','Retired')");
                });

            migrationBuilder.CreateTable(
                name: "VehicleCategories",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    VehicleFamilyCode = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    LifecycleStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    PublicationStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ContentRevision = table.Column<int>(type: "int", nullable: false),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirementReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCategories", x => x.Id);
                    table.CheckConstraint("CK_VehicleCategories_Family", "[VehicleFamilyCode] IN ('Motorcycle','Car')");
                    table.CheckConstraint("CK_VehicleCategories_Lifecycle", "[LifecycleStatus] IN ('Active','Retired')");
                    table.CheckConstraint("CK_VehicleCategories_Order", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_VehicleCategories_Publication", "[PublicationStatus] IN ('Draft','Published','Hidden')");
                    table.CheckConstraint("CK_VehicleCategories_Revision", "[ContentRevision] > 0");
                    table.CheckConstraint("CK_VehicleCategory_CodeCanonical", "DATALENGTH([Code]) > 0 AND [Code] = LOWER([Code]) AND CHARINDEX(' ', [Code]) = 0 AND CHARINDEX(CHAR(9), [Code]) = 0 AND CHARINDEX(CHAR(10), [Code]) = 0 AND CHARINDEX(CHAR(11), [Code]) = 0 AND CHARINDEX(CHAR(12), [Code]) = 0 AND CHARINDEX(CHAR(13), [Code]) = 0");
                });

            migrationBuilder.CreateTable(
                name: "VehicleCategoryTranslations",
                schema: "sangrental",
                columns: table => new
                {
                    VehicleCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Slug = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false),
                    TranslationStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    SourceRevision = table.Column<int>(type: "int", nullable: false),
                    ReviewedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCategoryTranslations", x => new { x.VehicleCategoryId, x.Locale });
                    table.CheckConstraint("CK_VehicleCategoryTranslation_Locale", "[Locale] IN ('vi','en','ko','zh-CN','ja','th')");
                    table.ForeignKey(
                        name: "FK_VehicleCategoryTranslations_VehicleCategories_VehicleCategoryId",
                        column: x => x.VehicleCategoryId,
                        principalSchema: "sangrental",
                        principalTable: "VehicleCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VehicleModels",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    VehicleBrandId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    CommercialName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EditionLabel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ModelYear = table.Column<short>(type: "smallint", nullable: true),
                    ContentRevision = table.Column<int>(type: "int", nullable: false),
                    LifecycleStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    PublicationStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirementReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModels", x => x.Id);
                    table.CheckConstraint("CK_VehicleModel_CodeCanonical", "DATALENGTH([Code]) > 0 AND [Code] = LOWER([Code]) AND CHARINDEX(' ', [Code]) = 0 AND CHARINDEX(CHAR(9), [Code]) = 0 AND CHARINDEX(CHAR(10), [Code]) = 0 AND CHARINDEX(CHAR(11), [Code]) = 0 AND CHARINDEX(CHAR(12), [Code]) = 0 AND CHARINDEX(CHAR(13), [Code]) = 0");
                    table.CheckConstraint("CK_VehicleModels_Lifecycle", "[LifecycleStatus] IN ('Active','Retired')");
                    table.CheckConstraint("CK_VehicleModels_Order", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_VehicleModels_Publication", "[PublicationStatus] IN ('Draft','Published','Hidden')");
                    table.CheckConstraint("CK_VehicleModels_Revision", "[ContentRevision] > 0");
                    table.ForeignKey(
                        name: "FK_VehicleModels_VehicleBrands_VehicleBrandId",
                        column: x => x.VehicleBrandId,
                        principalSchema: "sangrental",
                        principalTable: "VehicleBrands",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VehicleModels_VehicleCategories_VehicleCategoryId",
                        column: x => x.VehicleCategoryId,
                        principalSchema: "sangrental",
                        principalTable: "VehicleCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhysicalVehicles",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleModelId = table.Column<long>(type: "bigint", nullable: false),
                    AssetCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ManufacturingYear = table.Column<short>(type: "smallint", nullable: true),
                    ColorName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    VinOrChassisNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NormalizedVinOrChassisNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    RecordLifecycle = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    OperationalStatus = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    LastReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadyForRentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirementReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalVehicles", x => x.Id);
                    table.CheckConstraint("CK_PhysicalVehicles_OperationalStatus", "[OperationalStatus] IN ('PendingInspection','Ready','ReturnInspection','Maintenance','Suspended','LostLocked','AccidentLocked')");
                    table.CheckConstraint("CK_PhysicalVehicles_RecordLifecycle", "[RecordLifecycle] IN ('Draft','Active','Retired')");
                    table.CheckConstraint("CK_PhysicalVehicles_VinPair", "([VinOrChassisNumber] IS NULL AND [NormalizedVinOrChassisNumber] IS NULL) OR ([VinOrChassisNumber] IS NOT NULL AND [NormalizedVinOrChassisNumber] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PhysicalVehicles_VehicleModels_VehicleModelId",
                        column: x => x.VehicleModelId,
                        principalSchema: "sangrental",
                        principalTable: "VehicleModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VehicleModelImages",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleModelId = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ContentHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    MediaType = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RightsReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModelImages", x => x.Id);
                    table.CheckConstraint("CK_VehicleModelImages_Approval", "[ApprovalStatus] IN ('Draft','Approved','Rejected')");
                    table.CheckConstraint("CK_VehicleModelImages_Order", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_VehicleModelImages_Size", "[Width] > 0 AND [Height] > 0");
                    table.ForeignKey(
                        name: "FK_VehicleModelImages_VehicleModels_VehicleModelId",
                        column: x => x.VehicleModelId,
                        principalSchema: "sangrental",
                        principalTable: "VehicleModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VehicleModelSpecifications",
                schema: "sangrental",
                columns: table => new
                {
                    VehicleModelId = table.Column<long>(type: "bigint", nullable: false),
                    SeatCount = table.Column<byte>(type: "tinyint", nullable: true),
                    EngineDisplacementCc = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    FuelConsumptionLitresPer100Km = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    LuggageCapacityLitres = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    TransmissionCode = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: true),
                    EnergyTypeCode = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: true),
                    HasAbs = table.Column<bool>(type: "bit", nullable: true),
                    SeatCountSourceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EngineDisplacementSourceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasAbsSourceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VerificationStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModelSpecifications", x => x.VehicleModelId);
                    table.CheckConstraint("CK_Specifications_Engine", "[EngineDisplacementCc] IS NULL OR [EngineDisplacementCc] > 0");
                    table.CheckConstraint("CK_Specifications_Fuel", "[FuelConsumptionLitresPer100Km] IS NULL OR [FuelConsumptionLitresPer100Km] > 0");
                    table.CheckConstraint("CK_Specifications_SeatCount", "[SeatCount] IS NULL OR [SeatCount] > 0");
                    table.ForeignKey(
                        name: "FK_VehicleModelSpecifications_VehicleModels_VehicleModelId",
                        column: x => x.VehicleModelId,
                        principalSchema: "sangrental",
                        principalTable: "VehicleModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VehicleModelTranslations",
                schema: "sangrental",
                columns: table => new
                {
                    VehicleModelId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false, collation: "Latin1_General_100_BIN2"),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Slug = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SeoDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TranslationStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    SourceRevision = table.Column<int>(type: "int", nullable: false),
                    ReviewedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModelTranslations", x => new { x.VehicleModelId, x.Locale });
                    table.CheckConstraint("CK_VehicleModelTranslation_Locale", "[Locale] IN ('vi','en','ko','zh-CN','ja','th')");
                    table.ForeignKey(
                        name: "FK_VehicleModelTranslations_VehicleModels_VehicleModelId",
                        column: x => x.VehicleModelId,
                        principalSchema: "sangrental",
                        principalTable: "VehicleModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VehicleInspections",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhysicalVehicleId = table.Column<long>(type: "bigint", nullable: false),
                    InspectionTypeCode = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    ResultCode = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    InspectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InspectorReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OdometerKm = table.Column<decimal>(type: "decimal(12,1)", precision: 12, scale: 1, nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EvidenceReference = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleInspections", x => x.Id);
                    table.CheckConstraint("CK_VehicleInspections_Result", "[ResultCode] IN ('Passed','Failed','NeedsReview')");
                    table.ForeignKey(
                        name: "FK_VehicleInspections_PhysicalVehicles_PhysicalVehicleId",
                        column: x => x.PhysicalVehicleId,
                        principalSchema: "sangrental",
                        principalTable: "PhysicalVehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VehicleOperationalStatusHistories",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhysicalVehicleId = table.Column<long>(type: "bigint", nullable: false),
                    PreviousStatus = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    NewStatus = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalEventId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    SourceCode = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleOperationalStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleOperationalStatusHistories_PhysicalVehicles_PhysicalVehicleId",
                        column: x => x.PhysicalVehicleId,
                        principalSchema: "sangrental",
                        principalTable: "PhysicalVehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VehicleRegistrations",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhysicalVehicleId = table.Column<long>(type: "bigint", nullable: false),
                    RegistrationJurisdictionCode = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    PlateDisplay = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NormalizedPlate = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ClosureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleRegistrations", x => x.Id);
                    table.CheckConstraint("CK_VehicleRegistrations_Interval", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
                    table.ForeignKey(
                        name: "FK_VehicleRegistrations_PhysicalVehicles_PhysicalVehicleId",
                        column: x => x.PhysicalVehicleId,
                        principalSchema: "sangrental",
                        principalTable: "PhysicalVehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VehicleModelImageTranslations",
                schema: "sangrental",
                columns: table => new
                {
                    VehicleModelImageId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false, collation: "Latin1_General_100_BIN2"),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TranslationStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    SourceRevision = table.Column<int>(type: "int", nullable: false),
                    ReviewedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModelImageTranslations", x => new { x.VehicleModelImageId, x.Locale });
                    table.CheckConstraint("CK_ImageTranslation_Locale", "[Locale] IN ('vi','en','ko','zh-CN','ja','th')");
                    table.ForeignKey(
                        name: "FK_VehicleModelImageTranslations_VehicleModelImages_VehicleModelImageId",
                        column: x => x.VehicleModelImageId,
                        principalSchema: "sangrental",
                        principalTable: "VehicleModelImages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceEpisodes",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhysicalVehicleId = table.Column<long>(type: "bigint", nullable: false),
                    ScheduledStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusCode = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReleaseInspectionId = table.Column<long>(type: "bigint", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceEpisodes", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceEpisodes_ActualInterval", "[ActualEndUtc] IS NULL OR ([ActualStartUtc] IS NOT NULL AND [ActualEndUtc] >= [ActualStartUtc])");
                    table.CheckConstraint("CK_MaintenanceEpisodes_PlannedInterval", "[ScheduledEndUtc] IS NULL OR [ScheduledEndUtc] > [ScheduledStartUtc]");
                    table.CheckConstraint("CK_MaintenanceEpisodes_Status", "[StatusCode] IN ('Planned','InProgress','Completed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_MaintenanceEpisodes_PhysicalVehicles_PhysicalVehicleId",
                        column: x => x.PhysicalVehicleId,
                        principalSchema: "sangrental",
                        principalTable: "PhysicalVehicles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceEpisodes_VehicleInspections_ReleaseInspectionId",
                        column: x => x.ReleaseInspectionId,
                        principalSchema: "sangrental",
                        principalTable: "VehicleInspections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklistItems",
                schema: "sangrental",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaintenanceEpisodeId = table.Column<long>(type: "bigint", nullable: false),
                    TaskTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    TitleSnapshotVi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StatusCode = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SkipReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChangedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedByReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceChecklistItems", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceChecklistItems_Status", "[StatusCode] IN ('Pending','Completed','Skipped')");
                    table.ForeignKey(
                        name: "FK_MaintenanceChecklistItems_MaintenanceEpisodes_MaintenanceEpisodeId",
                        column: x => x.MaintenanceEpisodeId,
                        principalSchema: "sangrental",
                        principalTable: "MaintenanceEpisodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceChecklistItems_MaintenanceTaskTemplates_TaskTemplateId",
                        column: x => x.TaskTemplateId,
                        principalSchema: "sangrental",
                        principalTable: "MaintenanceTaskTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogFleetAuditEvents_EntityType_EntityId_OccurredAtUtc",
                schema: "sangrental",
                table: "CatalogFleetAuditEvents",
                columns: new[] { "EntityType", "EntityId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklistItems_MaintenanceEpisodeId_Id",
                schema: "sangrental",
                table: "MaintenanceChecklistItems",
                columns: new[] { "MaintenanceEpisodeId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklistItems_TaskTemplateId",
                schema: "sangrental",
                table: "MaintenanceChecklistItems",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceEpisodes_PhysicalVehicleId_ScheduledStartUtc",
                schema: "sangrental",
                table: "MaintenanceEpisodes",
                columns: new[] { "PhysicalVehicleId", "ScheduledStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceEpisodes_ReleaseInspectionId",
                schema: "sangrental",
                table: "MaintenanceEpisodes",
                column: "ReleaseInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTaskTemplates_Code",
                schema: "sangrental",
                table: "MaintenanceTaskTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVehicles_AssetCode",
                schema: "sangrental",
                table: "PhysicalVehicles",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVehicles_NormalizedVinOrChassisNumber",
                schema: "sangrental",
                table: "PhysicalVehicles",
                column: "NormalizedVinOrChassisNumber",
                filter: "[NormalizedVinOrChassisNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVehicles_VehicleModelId_RecordLifecycle_OperationalStatus",
                schema: "sangrental",
                table: "PhysicalVehicles",
                columns: new[] { "VehicleModelId", "RecordLifecycle", "OperationalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBrands_Code",
                schema: "sangrental",
                table: "VehicleBrands",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCategories_Code",
                schema: "sangrental",
                table: "VehicleCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCategoryTranslations_Locale_Slug",
                schema: "sangrental",
                table: "VehicleCategoryTranslations",
                columns: new[] { "Locale", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleInspections_PhysicalVehicleId_InspectedAtUtc",
                schema: "sangrental",
                table: "VehicleInspections",
                columns: new[] { "PhysicalVehicleId", "InspectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModelImages_VehicleModelId",
                schema: "sangrental",
                table: "VehicleModelImages",
                column: "VehicleModelId",
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_Code",
                schema: "sangrental",
                table: "VehicleModels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_VehicleBrandId",
                schema: "sangrental",
                table: "VehicleModels",
                column: "VehicleBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_VehicleCategoryId_PublicationStatus",
                schema: "sangrental",
                table: "VehicleModels",
                columns: new[] { "VehicleCategoryId", "PublicationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModelTranslations_Locale_Slug",
                schema: "sangrental",
                table: "VehicleModelTranslations",
                columns: new[] { "Locale", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationalStatusHistories_PhysicalVehicleId",
                schema: "sangrental",
                table: "VehicleOperationalStatusHistories",
                column: "PhysicalVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationalStatusHistories_SourceCode_ExternalEventId",
                schema: "sangrental",
                table: "VehicleOperationalStatusHistories",
                columns: new[] { "SourceCode", "ExternalEventId" },
                unique: true,
                filter: "[ExternalEventId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRegistrations_PhysicalVehicleId",
                schema: "sangrental",
                table: "VehicleRegistrations",
                column: "PhysicalVehicleId",
                unique: true,
                filter: "[ValidToUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRegistrations_RegistrationJurisdictionCode_NormalizedPlate",
                schema: "sangrental",
                table: "VehicleRegistrations",
                columns: new[] { "RegistrationJurisdictionCode", "NormalizedPlate" },
                unique: true,
                filter: "[ValidToUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogFleetAuditEvents",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "MaintenanceChecklistItems",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleCategoryTranslations",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleModelImageTranslations",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleModelSpecifications",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleModelTranslations",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleOperationalStatusHistories",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleRegistrations",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "MaintenanceEpisodes",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "MaintenanceTaskTemplates",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleModelImages",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleInspections",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "PhysicalVehicles",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleModels",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleBrands",
                schema: "sangrental");

            migrationBuilder.DropTable(
                name: "VehicleCategories",
                schema: "sangrental");
        }
    }
}
