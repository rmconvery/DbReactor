BEGIN TRANSACTION;
GO

-- Environment-specific configuration table
CREATE TABLE ${Environment}_Configuration (
    ConfigId INT PRIMARY KEY IDENTITY(1,1),
    ConfigKey NVARCHAR(100) NOT NULL,
    ConfigValue NVARCHAR(500) NOT NULL,
    TenantId NVARCHAR(50) DEFAULT '${TenantId}',
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Insert environment-specific configuration
INSERT INTO ${Environment}_Configuration (ConfigKey, ConfigValue, TenantId) VALUES 
    ('Environment', '${Environment}', '${TenantId}'),
    ('AdminEmail', '${AdminEmail}', '${TenantId}'),
    ('DatabaseMode', 'Migration', '${TenantId}');

GO

-- Create index for tenant lookups
CREATE INDEX IX_${Environment}_Configuration_TenantId ON ${Environment}_Configuration(TenantId);
GO