BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    IF SCHEMA_ID(N'sangrental') IS NULL EXEC(N'CREATE SCHEMA [sangrental];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[CatalogFleetAuditEvents] (
        [Id] bigint NOT NULL IDENTITY,
        [OccurredAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ActorReference] nvarchar(128) NOT NULL,
        [EntityType] varchar(64) NOT NULL,
        [EntityId] bigint NOT NULL,
        [ActionCode] varchar(64) NOT NULL,
        [Reason] nvarchar(1000) NULL,
        [CorrelationId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_CatalogFleetAuditEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[MaintenanceTaskTemplates] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [TitleVi] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        [UpdatedAtUtc] datetime2(7) NULL,
        [UpdatedByReference] nvarchar(128) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_MaintenanceTaskTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MaintenanceTaskTemplate_CodeCanonical] CHECK (DATALENGTH([Code]) > 0 AND [Code] = LOWER([Code]) AND CHARINDEX(' ', [Code]) = 0 AND CHARINDEX(CHAR(9), [Code]) = 0 AND CHARINDEX(CHAR(10), [Code]) = 0 AND CHARINDEX(CHAR(11), [Code]) = 0 AND CHARINDEX(CHAR(12), [Code]) = 0 AND CHARINDEX(CHAR(13), [Code]) = 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleBrands] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [DisplayName] nvarchar(160) NOT NULL,
        [LifecycleStatus] varchar(16) NOT NULL,
        [RetiredAtUtc] datetime2 NULL,
        [RetirementReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        [UpdatedAtUtc] datetime2(7) NULL,
        [UpdatedByReference] nvarchar(128) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VehicleBrands] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleBrand_CodeCanonical] CHECK (DATALENGTH([Code]) > 0 AND [Code] = LOWER([Code]) AND CHARINDEX(' ', [Code]) = 0 AND CHARINDEX(CHAR(9), [Code]) = 0 AND CHARINDEX(CHAR(10), [Code]) = 0 AND CHARINDEX(CHAR(11), [Code]) = 0 AND CHARINDEX(CHAR(12), [Code]) = 0 AND CHARINDEX(CHAR(13), [Code]) = 0),
        CONSTRAINT [CK_VehicleBrands_Lifecycle] CHECK ([LifecycleStatus] IN ('Active','Retired'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleCategories] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [VehicleFamilyCode] varchar(16) NOT NULL,
        [LifecycleStatus] varchar(16) NOT NULL,
        [PublicationStatus] varchar(16) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [ContentRevision] int NOT NULL,
        [RetiredAtUtc] datetime2 NULL,
        [RetirementReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        [UpdatedAtUtc] datetime2(7) NULL,
        [UpdatedByReference] nvarchar(128) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VehicleCategories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleCategories_Family] CHECK ([VehicleFamilyCode] IN ('Motorcycle','Car')),
        CONSTRAINT [CK_VehicleCategories_Lifecycle] CHECK ([LifecycleStatus] IN ('Active','Retired')),
        CONSTRAINT [CK_VehicleCategories_Order] CHECK ([DisplayOrder] >= 0),
        CONSTRAINT [CK_VehicleCategories_Publication] CHECK ([PublicationStatus] IN ('Draft','Published','Hidden')),
        CONSTRAINT [CK_VehicleCategories_Revision] CHECK ([ContentRevision] > 0),
        CONSTRAINT [CK_VehicleCategory_CodeCanonical] CHECK (DATALENGTH([Code]) > 0 AND [Code] = LOWER([Code]) AND CHARINDEX(' ', [Code]) = 0 AND CHARINDEX(CHAR(9), [Code]) = 0 AND CHARINDEX(CHAR(10), [Code]) = 0 AND CHARINDEX(CHAR(11), [Code]) = 0 AND CHARINDEX(CHAR(12), [Code]) = 0 AND CHARINDEX(CHAR(13), [Code]) = 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleCategoryTranslations] (
        [VehicleCategoryId] bigint NOT NULL,
        [Locale] varchar(5) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Slug] varchar(160) NOT NULL,
        [TranslationStatus] varchar(16) NOT NULL,
        [SourceRevision] int NOT NULL,
        [ReviewedByReference] nvarchar(128) NULL,
        [ReviewedAtUtc] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VehicleCategoryTranslations] PRIMARY KEY ([VehicleCategoryId], [Locale]),
        CONSTRAINT [CK_VehicleCategoryTranslation_Locale] CHECK ([Locale] IN ('vi','en','ko','zh-CN','ja','th')),
        CONSTRAINT [FK_VehicleCategoryTranslations_VehicleCategories_VehicleCategoryId] FOREIGN KEY ([VehicleCategoryId]) REFERENCES [sangrental].[VehicleCategories] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleModels] (
        [Id] bigint NOT NULL IDENTITY,
        [VehicleCategoryId] bigint NOT NULL,
        [VehicleBrandId] bigint NOT NULL,
        [Code] varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [CommercialName] nvarchar(160) NOT NULL,
        [EditionLabel] nvarchar(80) NULL,
        [ModelYear] smallint NULL,
        [ContentRevision] int NOT NULL,
        [LifecycleStatus] varchar(16) NOT NULL,
        [PublicationStatus] varchar(16) NOT NULL,
        [PublishedAtUtc] datetime2 NULL,
        [PublishedByReference] nvarchar(128) NULL,
        [IsFeatured] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [RetiredAtUtc] datetime2 NULL,
        [RetirementReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        [UpdatedAtUtc] datetime2(7) NULL,
        [UpdatedByReference] nvarchar(128) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VehicleModels] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleModel_CodeCanonical] CHECK (DATALENGTH([Code]) > 0 AND [Code] = LOWER([Code]) AND CHARINDEX(' ', [Code]) = 0 AND CHARINDEX(CHAR(9), [Code]) = 0 AND CHARINDEX(CHAR(10), [Code]) = 0 AND CHARINDEX(CHAR(11), [Code]) = 0 AND CHARINDEX(CHAR(12), [Code]) = 0 AND CHARINDEX(CHAR(13), [Code]) = 0),
        CONSTRAINT [CK_VehicleModels_Lifecycle] CHECK ([LifecycleStatus] IN ('Active','Retired')),
        CONSTRAINT [CK_VehicleModels_Order] CHECK ([DisplayOrder] >= 0),
        CONSTRAINT [CK_VehicleModels_Publication] CHECK ([PublicationStatus] IN ('Draft','Published','Hidden')),
        CONSTRAINT [CK_VehicleModels_Revision] CHECK ([ContentRevision] > 0),
        CONSTRAINT [FK_VehicleModels_VehicleBrands_VehicleBrandId] FOREIGN KEY ([VehicleBrandId]) REFERENCES [sangrental].[VehicleBrands] ([Id]),
        CONSTRAINT [FK_VehicleModels_VehicleCategories_VehicleCategoryId] FOREIGN KEY ([VehicleCategoryId]) REFERENCES [sangrental].[VehicleCategories] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[PhysicalVehicles] (
        [Id] bigint NOT NULL IDENTITY,
        [VehicleModelId] bigint NOT NULL,
        [AssetCode] varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [ManufacturingYear] smallint NULL,
        [ColorName] nvarchar(80) NULL,
        [VinOrChassisNumber] nvarchar(100) NULL,
        [NormalizedVinOrChassisNumber] varchar(100) NULL,
        [RecordLifecycle] varchar(16) NOT NULL,
        [OperationalStatus] varchar(32) NOT NULL,
        [LastReceivedAtUtc] datetime2 NULL,
        [ReadyForRentAtUtc] datetime2 NULL,
        [RetiredAtUtc] datetime2 NULL,
        [RetirementReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        [UpdatedAtUtc] datetime2(7) NULL,
        [UpdatedByReference] nvarchar(128) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PhysicalVehicles] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PhysicalVehicles_OperationalStatus] CHECK ([OperationalStatus] IN ('PendingInspection','Ready','ReturnInspection','Maintenance','Suspended','LostLocked','AccidentLocked')),
        CONSTRAINT [CK_PhysicalVehicles_RecordLifecycle] CHECK ([RecordLifecycle] IN ('Draft','Active','Retired')),
        CONSTRAINT [CK_PhysicalVehicles_VinPair] CHECK (([VinOrChassisNumber] IS NULL AND [NormalizedVinOrChassisNumber] IS NULL) OR ([VinOrChassisNumber] IS NOT NULL AND [NormalizedVinOrChassisNumber] IS NOT NULL)),
        CONSTRAINT [FK_PhysicalVehicles_VehicleModels_VehicleModelId] FOREIGN KEY ([VehicleModelId]) REFERENCES [sangrental].[VehicleModels] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleModelImages] (
        [Id] bigint NOT NULL IDENTITY,
        [VehicleModelId] bigint NOT NULL,
        [StorageKey] nvarchar(450) NOT NULL,
        [ContentHash] binary(32) NOT NULL,
        [MediaType] varchar(64) NOT NULL,
        [Width] int NOT NULL,
        [Height] int NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsPrimary] bit NOT NULL,
        [IsVisible] bit NOT NULL,
        [ApprovalStatus] varchar(16) NOT NULL,
        [SourceReference] nvarchar(500) NULL,
        [RightsReference] nvarchar(500) NULL,
        [ApprovedAtUtc] datetime2 NULL,
        [ApprovedByReference] nvarchar(128) NULL,
        [RetiredAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        [UpdatedAtUtc] datetime2(7) NULL,
        [UpdatedByReference] nvarchar(128) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VehicleModelImages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleModelImages_Approval] CHECK ([ApprovalStatus] IN ('Draft','Approved','Rejected')),
        CONSTRAINT [CK_VehicleModelImages_Order] CHECK ([DisplayOrder] >= 0),
        CONSTRAINT [CK_VehicleModelImages_Size] CHECK ([Width] > 0 AND [Height] > 0),
        CONSTRAINT [FK_VehicleModelImages_VehicleModels_VehicleModelId] FOREIGN KEY ([VehicleModelId]) REFERENCES [sangrental].[VehicleModels] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleModelSpecifications] (
        [VehicleModelId] bigint NOT NULL,
        [SeatCount] tinyint NULL,
        [EngineDisplacementCc] decimal(7,2) NULL,
        [FuelConsumptionLitresPer100Km] decimal(6,3) NULL,
        [LuggageCapacityLitres] decimal(8,2) NULL,
        [WeightKg] decimal(8,2) NULL,
        [TransmissionCode] varchar(24) NULL,
        [EnergyTypeCode] varchar(24) NULL,
        [HasAbs] bit NULL,
        [SeatCountSourceReference] nvarchar(500) NULL,
        [EngineDisplacementSourceReference] nvarchar(500) NULL,
        [HasAbsSourceReference] nvarchar(500) NULL,
        [VerificationStatus] varchar(16) NOT NULL,
        [VerifiedAtUtc] datetime2 NULL,
        [VerifiedByReference] nvarchar(128) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByReference] nvarchar(128) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VehicleModelSpecifications] PRIMARY KEY ([VehicleModelId]),
        CONSTRAINT [CK_Specifications_Engine] CHECK ([EngineDisplacementCc] IS NULL OR [EngineDisplacementCc] > 0),
        CONSTRAINT [CK_Specifications_Fuel] CHECK ([FuelConsumptionLitresPer100Km] IS NULL OR [FuelConsumptionLitresPer100Km] > 0),
        CONSTRAINT [CK_Specifications_SeatCount] CHECK ([SeatCount] IS NULL OR [SeatCount] > 0),
        CONSTRAINT [FK_VehicleModelSpecifications_VehicleModels_VehicleModelId] FOREIGN KEY ([VehicleModelId]) REFERENCES [sangrental].[VehicleModels] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleModelTranslations] (
        [VehicleModelId] bigint NOT NULL,
        [Locale] varchar(5) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Summary] nvarchar(1000) NULL,
        [Description] nvarchar(max) NULL,
        [Slug] varchar(160) NOT NULL,
        [SeoTitle] nvarchar(200) NULL,
        [SeoDescription] nvarchar(500) NULL,
        [TranslationStatus] varchar(16) NOT NULL,
        [SourceRevision] int NOT NULL,
        [ReviewedByReference] nvarchar(128) NULL,
        [ReviewedAtUtc] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VehicleModelTranslations] PRIMARY KEY ([VehicleModelId], [Locale]),
        CONSTRAINT [CK_VehicleModelTranslation_Locale] CHECK ([Locale] IN ('vi','en','ko','zh-CN','ja','th')),
        CONSTRAINT [FK_VehicleModelTranslations_VehicleModels_VehicleModelId] FOREIGN KEY ([VehicleModelId]) REFERENCES [sangrental].[VehicleModels] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleInspections] (
        [Id] bigint NOT NULL IDENTITY,
        [PhysicalVehicleId] bigint NOT NULL,
        [InspectionTypeCode] varchar(32) NOT NULL,
        [ResultCode] varchar(16) NOT NULL,
        [InspectedAtUtc] datetime2 NOT NULL,
        [InspectorReference] nvarchar(128) NOT NULL,
        [OdometerKm] decimal(12,1) NULL,
        [Findings] nvarchar(2000) NULL,
        [EvidenceReference] nvarchar(450) NULL,
        [SourceReference] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        CONSTRAINT [PK_VehicleInspections] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleInspections_Result] CHECK ([ResultCode] IN ('Passed','Failed','NeedsReview')),
        CONSTRAINT [FK_VehicleInspections_PhysicalVehicles_PhysicalVehicleId] FOREIGN KEY ([PhysicalVehicleId]) REFERENCES [sangrental].[PhysicalVehicles] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleOperationalStatusHistories] (
        [Id] bigint NOT NULL IDENTITY,
        [PhysicalVehicleId] bigint NOT NULL,
        [PreviousStatus] varchar(32) NULL,
        [NewStatus] varchar(32) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [CorrelationId] uniqueidentifier NOT NULL,
        [ExternalEventId] varchar(128) NULL,
        [SourceCode] varchar(32) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        CONSTRAINT [PK_VehicleOperationalStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VehicleOperationalStatusHistories_PhysicalVehicles_PhysicalVehicleId] FOREIGN KEY ([PhysicalVehicleId]) REFERENCES [sangrental].[PhysicalVehicles] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleRegistrations] (
        [Id] bigint NOT NULL IDENTITY,
        [PhysicalVehicleId] bigint NOT NULL,
        [RegistrationJurisdictionCode] varchar(32) NOT NULL,
        [PlateDisplay] nvarchar(32) NOT NULL,
        [NormalizedPlate] varchar(32) NOT NULL,
        [ValidFromUtc] datetime2 NOT NULL,
        [ValidToUtc] datetime2 NULL,
        [ClosedAtUtc] datetime2 NULL,
        [ClosedByReference] nvarchar(128) NULL,
        [ClosureReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VehicleRegistrations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleRegistrations_Interval] CHECK ([ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]),
        CONSTRAINT [FK_VehicleRegistrations_PhysicalVehicles_PhysicalVehicleId] FOREIGN KEY ([PhysicalVehicleId]) REFERENCES [sangrental].[PhysicalVehicles] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[VehicleModelImageTranslations] (
        [VehicleModelImageId] bigint NOT NULL,
        [Locale] varchar(5) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [AltText] nvarchar(300) NOT NULL,
        [Caption] nvarchar(1000) NULL,
        [TranslationStatus] varchar(16) NOT NULL,
        [SourceRevision] int NOT NULL,
        [ReviewedByReference] nvarchar(128) NULL,
        [ReviewedAtUtc] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VehicleModelImageTranslations] PRIMARY KEY ([VehicleModelImageId], [Locale]),
        CONSTRAINT [CK_ImageTranslation_Locale] CHECK ([Locale] IN ('vi','en','ko','zh-CN','ja','th')),
        CONSTRAINT [FK_VehicleModelImageTranslations_VehicleModelImages_VehicleModelImageId] FOREIGN KEY ([VehicleModelImageId]) REFERENCES [sangrental].[VehicleModelImages] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[MaintenanceEpisodes] (
        [Id] bigint NOT NULL IDENTITY,
        [PhysicalVehicleId] bigint NOT NULL,
        [ScheduledStartUtc] datetime2 NOT NULL,
        [ScheduledEndUtc] datetime2 NULL,
        [ActualStartUtc] datetime2 NULL,
        [ActualEndUtc] datetime2 NULL,
        [StatusCode] varchar(16) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [ReleasedAtUtc] datetime2 NULL,
        [ReleasedByReference] nvarchar(128) NULL,
        [ReleaseInspectionId] bigint NULL,
        [CancellationReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        [UpdatedAtUtc] datetime2(7) NULL,
        [UpdatedByReference] nvarchar(128) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_MaintenanceEpisodes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MaintenanceEpisodes_ActualInterval] CHECK ([ActualEndUtc] IS NULL OR ([ActualStartUtc] IS NOT NULL AND [ActualEndUtc] >= [ActualStartUtc])),
        CONSTRAINT [CK_MaintenanceEpisodes_PlannedInterval] CHECK ([ScheduledEndUtc] IS NULL OR [ScheduledEndUtc] > [ScheduledStartUtc]),
        CONSTRAINT [CK_MaintenanceEpisodes_Status] CHECK ([StatusCode] IN ('Planned','InProgress','Completed','Cancelled')),
        CONSTRAINT [FK_MaintenanceEpisodes_PhysicalVehicles_PhysicalVehicleId] FOREIGN KEY ([PhysicalVehicleId]) REFERENCES [sangrental].[PhysicalVehicles] ([Id]),
        CONSTRAINT [FK_MaintenanceEpisodes_VehicleInspections_ReleaseInspectionId] FOREIGN KEY ([ReleaseInspectionId]) REFERENCES [sangrental].[VehicleInspections] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE TABLE [sangrental].[MaintenanceChecklistItems] (
        [Id] bigint NOT NULL IDENTITY,
        [MaintenanceEpisodeId] bigint NOT NULL,
        [TaskTemplateId] bigint NULL,
        [TitleSnapshotVi] nvarchar(200) NOT NULL,
        [StatusCode] varchar(16) NOT NULL,
        [Note] nvarchar(2000) NULL,
        [SkipReason] nvarchar(1000) NULL,
        [ChangedAtUtc] datetime2 NULL,
        [ChangedByReference] nvarchar(128) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByReference] nvarchar(128) NOT NULL,
        [UpdatedAtUtc] datetime2(7) NULL,
        [UpdatedByReference] nvarchar(128) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_MaintenanceChecklistItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MaintenanceChecklistItems_Status] CHECK ([StatusCode] IN ('Pending','Completed','Skipped')),
        CONSTRAINT [FK_MaintenanceChecklistItems_MaintenanceEpisodes_MaintenanceEpisodeId] FOREIGN KEY ([MaintenanceEpisodeId]) REFERENCES [sangrental].[MaintenanceEpisodes] ([Id]),
        CONSTRAINT [FK_MaintenanceChecklistItems_MaintenanceTaskTemplates_TaskTemplateId] FOREIGN KEY ([TaskTemplateId]) REFERENCES [sangrental].[MaintenanceTaskTemplates] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE INDEX [IX_CatalogFleetAuditEvents_EntityType_EntityId_OccurredAtUtc] ON [sangrental].[CatalogFleetAuditEvents] ([EntityType], [EntityId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE INDEX [IX_MaintenanceChecklistItems_MaintenanceEpisodeId_Id] ON [sangrental].[MaintenanceChecklistItems] ([MaintenanceEpisodeId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE INDEX [IX_MaintenanceChecklistItems_TaskTemplateId] ON [sangrental].[MaintenanceChecklistItems] ([TaskTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE INDEX [IX_MaintenanceEpisodes_PhysicalVehicleId_ScheduledStartUtc] ON [sangrental].[MaintenanceEpisodes] ([PhysicalVehicleId], [ScheduledStartUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE INDEX [IX_MaintenanceEpisodes_ReleaseInspectionId] ON [sangrental].[MaintenanceEpisodes] ([ReleaseInspectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MaintenanceTaskTemplates_Code] ON [sangrental].[MaintenanceTaskTemplates] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PhysicalVehicles_AssetCode] ON [sangrental].[PhysicalVehicles] ([AssetCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_PhysicalVehicles_NormalizedVinOrChassisNumber] ON [sangrental].[PhysicalVehicles] ([NormalizedVinOrChassisNumber]) WHERE [NormalizedVinOrChassisNumber] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE INDEX [IX_PhysicalVehicles_VehicleModelId_RecordLifecycle_OperationalStatus] ON [sangrental].[PhysicalVehicles] ([VehicleModelId], [RecordLifecycle], [OperationalStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleBrands_Code] ON [sangrental].[VehicleBrands] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleCategories_Code] ON [sangrental].[VehicleCategories] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleCategoryTranslations_Locale_Slug] ON [sangrental].[VehicleCategoryTranslations] ([Locale], [Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE INDEX [IX_VehicleInspections_PhysicalVehicleId_InspectedAtUtc] ON [sangrental].[VehicleInspections] ([PhysicalVehicleId], [InspectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehicleModelImages_VehicleModelId] ON [sangrental].[VehicleModelImages] ([VehicleModelId]) WHERE [IsPrimary] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleModels_Code] ON [sangrental].[VehicleModels] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE INDEX [IX_VehicleModels_VehicleBrandId] ON [sangrental].[VehicleModels] ([VehicleBrandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE INDEX [IX_VehicleModels_VehicleCategoryId_PublicationStatus] ON [sangrental].[VehicleModels] ([VehicleCategoryId], [PublicationStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleModelTranslations_Locale_Slug] ON [sangrental].[VehicleModelTranslations] ([Locale], [Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    CREATE INDEX [IX_VehicleOperationalStatusHistories_PhysicalVehicleId] ON [sangrental].[VehicleOperationalStatusHistories] ([PhysicalVehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehicleOperationalStatusHistories_SourceCode_ExternalEventId] ON [sangrental].[VehicleOperationalStatusHistories] ([SourceCode], [ExternalEventId]) WHERE [ExternalEventId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehicleRegistrations_PhysicalVehicleId] ON [sangrental].[VehicleRegistrations] ([PhysicalVehicleId]) WHERE [ValidToUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehicleRegistrations_RegistrationJurisdictionCode_NormalizedPlate] ON [sangrental].[VehicleRegistrations] ([RegistrationJurisdictionCode], [NormalizedPlate]) WHERE [ValidToUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107173802_CatalogFleetInitial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107173802_CatalogFleetInitial', N'10.0.13');
END;

COMMIT;
GO

