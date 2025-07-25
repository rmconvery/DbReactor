IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'rules') IS NULL EXEC(N'CREATE SCHEMA [rules];');
GO

CREATE TABLE [rules].[OperatingUnits] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [CreatedBy] nvarchar(200) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT ((getdate())),
    [ModifiedBy] nvarchar(200) NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT (((1))),
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    CONSTRAINT [PK_OperatingUnits] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [rules].[Operators] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    CONSTRAINT [PK_Operators] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [rules].[RuleLog] (
    [Id] int NOT NULL IDENTITY,
    [OperatingUnitId] int NULL,
    [RuleSetId] int NULL,
    [RuleGroupId] int NULL,
    [TransactionId] uniqueidentifier NULL,
    [RuleIds] nvarchar(max) NULL,
    [Passed] bit NULL,
    [RuleMessages] nvarchar(max) NULL,
    [RuleDetails] nvarchar(max) NULL,
    [ExecutionTime] datetime2 NOT NULL DEFAULT ((getdate())),
    CONSTRAINT [PK_RuleLog] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [rules].[RuleSetObjects] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [JsonModel] nvarchar(max) NOT NULL,
    [CreatedBy] nvarchar(200) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT ((getdate())),
    [ModifiedBy] nvarchar(200) NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT (((1))),
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    CONSTRAINT [PK_RuleSetObjects] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [rules].[RuleSetType] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [CreatedBy] nvarchar(200) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT ((getdate())),
    [ModifiedBy] nvarchar(200) NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT (((1))),
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    CONSTRAINT [PK_RuleSetType] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [rules].[ResultActions] (
    [Id] int NOT NULL IDENTITY,
    [ActionType] int NOT NULL,
    [MessageTemplate] nvarchar(max) NOT NULL,
    [RuleSetObjectId] int NULL,
    CONSTRAINT [PK_ResultActions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ResultActions_RuleSetObjects_RuleSetObjectId] FOREIGN KEY ([RuleSetObjectId]) REFERENCES [rules].[RuleSetObjects] ([Id])
);
GO

CREATE TABLE [rules].[RuleSets] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [RuleSetType] nvarchar(max) NOT NULL,
    [RuleSetTypeId] int NULL,
    [OperatingUnitID] int NOT NULL,
    [CreatedBy] nvarchar(200) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT ((getdate())),
    [ModifiedBy] nvarchar(200) NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT (((1))),
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    CONSTRAINT [PK_RuleSets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RuleSets_OperatingUnits_OperatingUnitID] FOREIGN KEY ([OperatingUnitID]) REFERENCES [rules].[OperatingUnits] ([Id]),
    CONSTRAINT [FK_RuleSets_RuleSetType_RuleSetTypeId] FOREIGN KEY ([RuleSetTypeId]) REFERENCES [rules].[RuleSetType] ([Id])
);
GO

CREATE TABLE [rules].[RuleGroups] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Order] int NOT NULL,
    [CreatedBy] nvarchar(200) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT ((getdate())),
    [ModifiedBy] nvarchar(200) NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT (((1))),
    [DefaultResponse] nvarchar(max) NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [RuleSetId] int NOT NULL,
    [Operator] int NOT NULL,
    [ResultActionId] int NULL,
    CONSTRAINT [PK_RuleGroups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RuleGroups_ResultActions_ResultActionId] FOREIGN KEY ([ResultActionId]) REFERENCES [rules].[ResultActions] ([Id]),
    CONSTRAINT [FK_RuleGroups_RuleSets_RuleSetId] FOREIGN KEY ([RuleSetId]) REFERENCES [rules].[RuleSets] ([Id])
);
GO

CREATE TABLE [rules].[Rules] (
    [Id] int NOT NULL IDENTITY,
    [Order] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [RuleTarget] nvarchar(max) NOT NULL,
    [ComparisonOperator] int NOT NULL,
    [RuleValue] nvarchar(max) NOT NULL,
    [CreatedBy] nvarchar(200) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT ((getdate())),
    [ModifiedBy] nvarchar(200) NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT (((1))),
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [RuleGroupId] int NOT NULL,
    [ResponseMessage] nvarchar(max) NOT NULL,
    [RuleType] int NOT NULL,
    [RuleDataType] nvarchar(max) NOT NULL,
    [RuleResponseDataType] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Rules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Rules_RuleGroups_RuleGroupId] FOREIGN KEY ([RuleGroupId]) REFERENCES [rules].[RuleGroups] ([Id])
);
GO

CREATE INDEX [IX_ResultActions_RuleSetObjectId] ON [rules].[ResultActions] ([RuleSetObjectId]);
GO

CREATE INDEX [IX_RuleGroups_ResultActionId] ON [rules].[RuleGroups] ([ResultActionId]);
GO

CREATE INDEX [IX_RuleGroups_RuleSetId] ON [rules].[RuleGroups] ([RuleSetId]);
GO

CREATE INDEX [IX_Rules_RuleGroupId] ON [rules].[Rules] ([RuleGroupId]);
GO

CREATE INDEX [IX_RuleSets_OperatingUnitID] ON [rules].[RuleSets] ([OperatingUnitID]);
GO

CREATE INDEX [IX_RuleSets_RuleSetTypeId] ON [rules].[RuleSets] ([RuleSetTypeId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240221181246_initrules', N'8.0.1');
GO

COMMIT;
GO

