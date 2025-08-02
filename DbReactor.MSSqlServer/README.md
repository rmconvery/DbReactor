# DbReactor.MSSqlServer - SQL Server Database Provider

DbReactor.MSSqlServer is the SQL Server implementation for the DbReactor database migration framework. It provides full SQL Server support including connection management, script execution, migration journaling, and database provisioning.

## Installation

```bash
# Install both packages
dotnet add package DbReactor.Core
dotnet add package DbReactor.MSSqlServer
```

## Quick Start

```csharp
using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.MSSqlServer.Extensions;

var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseConsoleLogging()
    .CreateDatabaseIfNotExists();

var engine = new DbReactorEngine(config);

// Preview migrations before execution (dry run)
var dryRunResult = await engine.PreviewRunAsync();
Console.WriteLine($"Would execute {dryRunResult.PendingMigrations} migrations");

var result = await engine.RunAsync();

if (result.Successful)
{
    Console.WriteLine("Migrations completed successfully!");
}
else
{
    Console.WriteLine($"Migration failed: {result.ErrorMessage}");
}
```

## Configuration Options

### Basic Configuration

```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseConsoleLogging();
```

### Complete Configuration

```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(
        connectionString: "Server=localhost;Database=MyApp;Trusted_Connection=true;",
        commandTimeout: TimeSpan.FromMinutes(5),
        journalSchema: "migrations", 
        journalTable: "MigrationHistory"
    )
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseCodeScripts(typeof(Program).Assembly)
    .UseConsoleLogging()
    .CreateDatabaseIfNotExists()
    .UseVariables(new Dictionary<string, string> {
        {"Environment", "Production"},
        {"TenantId", "12345"}
    });
```

### Individual Component Configuration

```csharp
var config = new DbReactorConfiguration()
    .UseSqlServerConnection(connectionString)
    .UseSqlServerExecutor(TimeSpan.FromSeconds(60))
    .UseSqlServerJournal("dbo", "__migration_journal")
    .UseSqlServerProvisioner(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly);
```

## Timeout Configuration

Configure command timeouts using `TimeSpan` for better expressiveness:

```csharp
// Set timeout during initial configuration
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString, commandTimeout: TimeSpan.FromMinutes(5))
    .UseConsoleLogging();

// Or configure timeout separately
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseSqlServerCommandTimeout(TimeSpan.FromSeconds(120))
    .UseConsoleLogging();

// Common timeout examples
.UseSqlServer(connectionString, commandTimeout: TimeSpan.FromSeconds(30))     // 30 seconds
.UseSqlServer(connectionString, commandTimeout: TimeSpan.FromMinutes(2))      // 2 minutes
.UseSqlServer(connectionString, commandTimeout: TimeSpan.FromMilliseconds(500)) // 500ms
```

## Migration Journal

The migration journal tracks executed migrations in a SQL Server table (default: `__migration_journal`):

### Default Journal Configuration

```csharp
.UseSqlServer(connectionString) // Uses dbo.__migration_journal
```

### Custom Journal Configuration

```csharp
.UseSqlServer(connectionString, 
    journalSchema: "migrations",
    journalTable: "MigrationHistory")

// Or configure separately
.UseSqlServerJournal("custom_schema", "custom_table")
```

### Journal Table Schema

The journal table contains:
- `Id` - Identity primary key
- `UpgradeScriptHash` - Unique hash of the upgrade script
- `MigrationName` - Name of the migration
- `DowngradeScript` - Optional downgrade script content
- `MigratedOn` - UTC timestamp of execution
- `ExecutionTime` - Duration in milliseconds

## Database Creation

Automatically create databases if they don't exist:

```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .CreateDatabaseIfNotExists() // Enables automatic database creation
    .UseStandardFolderStructure(typeof(Program).Assembly);
```

### Custom Database Creation Template

```csharp
var creationTemplate = @"
    CREATE DATABASE [{0}] 
    COLLATE SQL_Latin1_General_CP1_CI_AS
    WITH TRUSTWORTHY ON";

var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .CreateDatabaseIfNotExists(creationTemplate)
    .UseStandardFolderStructure(typeof(Program).Assembly);
```

## Script Types

### SQL Scripts

Standard SQL migration files:

```sql
-- M001_CreateUsersTable.sql
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Users_Username ON Users(Username);
```

### C# Code Scripts

Dynamic C# migrations implementing `ICodeScript`:

```csharp
using DbReactor.Core.Abstractions;
using DbReactor.Core.Models.Contexts;

public class M002_SeedUsers : ICodeScript
{
    public bool SupportsDowngrade => true;

    public string GetUpgradeScript(CodeScriptContext context)
    {
        var env = context.Vars.GetString("Environment", "Development");
        var tenantId = context.Vars.GetRequiredString("TenantId");
        
        return $@"
            INSERT INTO Users (Username, Email, Environment, TenantId) 
            VALUES 
                ('admin', 'admin@company.com', '{env}', '{tenantId}'),
                ('user1', 'user1@company.com', '{env}', '{tenantId}')";
    }

    public string GetDowngradeScript(CodeScriptContext context)
    {
        var tenantId = context.Vars.GetRequiredString("TenantId");
        return $"DELETE FROM Users WHERE TenantId = '{tenantId}'";
    }
}
```

## Variable Substitution

Use variables in SQL scripts:

```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseVariables(new Dictionary<string, string> {
        {"Environment", "Production"},
        {"AdminEmail", "admin@company.com"},
        {"TenantId", "tenant-123"}
    });
```

In your SQL files:
```sql
INSERT INTO Configuration (Environment, AdminEmail, TenantId) 
VALUES ('${Environment}', '${AdminEmail}', '${TenantId}');
```

## File Structure

Organize your migration scripts:

```
YourProject/
├── Scripts/
│   ├── upgrades/
│   │   ├── M001_CreateUsersTable.sql
│   │   ├── M002_SeedUsers.cs
│   │   └── M003_CreateIndexes.sql
│   └── downgrades/
│       ├── M001_CreateUsersTable.sql
│       ├── M002_SeedUsers.sql
│       └── M003_CreateIndexes.sql
└── Program.cs
```

Mark SQL scripts as embedded resources in your `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Scripts\\**\\*.sql" />
</ItemGroup>
```

## Migration Naming

Use consistent naming for proper ordering:

✅ **Recommended:**
```
M001_CreateUsersTable.sql
M002_SeedUsers.cs
M003_CreateIndexes.sql
```

✅ **Also Good:**
```
20250716_001_CreateUsersTable.sql
20250716_002_SeedUsers.cs
V1_0_1_CreateUsersTable.sql
```

❌ **Avoid:**
```
001_CreateTable.sql + _002_SeedUsers.cs  // Inconsistent prefixes
createTable.sql + SeedUsers.cs          // No ordering
```

## Connection String Examples

### Windows Authentication
```csharp
var connectionString = "Server=localhost;Database=MyApp;Trusted_Connection=true;TrustServerCertificate=true;";
```

### SQL Server Authentication
```csharp
var connectionString = "Server=localhost;Database=MyApp;User Id=sa;Password=myPassword;TrustServerCertificate=true;";
```

### Azure SQL Database
```csharp
var connectionString = "Server=myserver.database.windows.net;Database=MyApp;User Id=myuser;Password=mypassword;";
```

## Advanced Features

### Multiple Script Sources
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseEmbeddedScriptsFromFolder(coreAssembly, "Core.Migrations", "upgrades")
    .UseCodeScripts(migrationAssembly, "Migrations.Code")
    .UseEmbeddedScriptsFromFolder(moduleAssembly, "Module.Scripts", "migrations");
```

### Custom Execution Order
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseAscendingOrder()   // M001, M002, M003... (default)
    .UseDescendingOrder(); // M003, M002, M001...
```

### Downgrade Support
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .AllowDowngrades(true);

// Execute downgrades
var result = await engine.RunDowngradeAsync();
```

## Error Handling

```csharp
var engine = new DbReactorEngine(config);

try
{
    var result = await engine.RunAsync();
    
    if (result.Successful)
    {
        Console.WriteLine("Migrations completed successfully!");
        Console.WriteLine($"Executed {result.ExecutedMigrations.Count()} migrations");
    }
    else
    {
        Console.WriteLine($"Migration failed: {result.ErrorMessage}");
        if (result.FailedMigration != null)
        {
            Console.WriteLine($"Failed migration: {result.FailedMigration.Name}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## SQL Server Data Seeding

DbReactor.MSSqlServer provides full seeding support with SQL Server-optimized features and journaling.

### Quick Seeding Setup

```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .EnableSqlServerSeeding(typeof(Program).Assembly)  // One-line seeding setup
    .UseConsoleLogging();

var engine = new DbReactorEngine(config);
var result = await engine.RunAsync();  // Runs migrations, then seeds
```

### Seeding Configuration Options

#### Basic Seeding
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .EnableSqlServerSeeding();  // Enable with defaults
```

#### Custom Schema and Table
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .EnableSqlServerSeeding("custom_schema", "my_seed_journal");
```

#### Multiple Seed Sources
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .EnableSqlServerSeeding()
    .UseEmbeddedSeeds(typeof(CoreModule).Assembly, "CoreData")
    .UseEmbeddedSeeds(typeof(AuthModule).Assembly, "AuthData")
    .UseFileSystemSeeds(@"C:\Seeds\Production");
```

### SQL Server Seed Examples

#### Using SQL Server Features
```sql
-- Seeds/run-once/S001_SeedUsers.sql
MERGE [Users] AS target
USING (VALUES 
    ('admin', 'admin@company.com', 'Administrator', NEWID()),
    ('guest', 'guest@company.com', 'ReadOnly', NEWID())
) AS source ([Username], [Email], [Role], [Id])
ON target.[Username] = source.[Username]
WHEN NOT MATCHED THEN 
    INSERT ([Id], [Username], [Email], [Role], [CreatedAt])
    VALUES (source.[Id], source.[Username], source.[Email], source.[Role], GETUTCDATE());
```

#### SQL Server Variables and Functions
```sql
-- Seeds/run-always/S002_UpdateStatistics.sql  
UPDATE [SystemStats] 
SET 
    [LastUpdated] = GETUTCDATE(),
    [ServerName] = @@SERVERNAME,
    [DatabaseName] = DB_NAME(),
    [Version] = @@VERSION;

-- Update row count statistics
UPDATE [TableStats] 
SET [RowCount] = (SELECT COUNT(*) FROM [Users])
WHERE [TableName] = 'Users';
```

#### Environment-Specific Seeds
```sql
-- Seeds/run-if-changed/S003_SeedConfiguration.sql
MERGE [Configuration] AS target
USING (VALUES 
    ('MaxLoginAttempts', '{{MaxLoginAttempts}}'),
    ('SessionTimeout', '{{SessionTimeout}}'),
    ('Environment', '{{Environment}}'),
    ('FeatureFlags.NewUI', '{{EnableNewUI}}')
) AS source ([Key], [Value])
ON target.[Key] = source.[Key]
WHEN MATCHED THEN 
    UPDATE SET [Value] = source.[Value], [UpdatedAt] = GETUTCDATE()
WHEN NOT MATCHED THEN 
    INSERT ([Key], [Value], [CreatedAt], [UpdatedAt])
    VALUES (source.[Key], source.[Value], GETUTCDATE(), GETUTCDATE());
```

### SQL Server Seed Journal

The seed journal is automatically created in SQL Server:

```sql
-- Default seed journal structure
CREATE TABLE [dbo].[__seed_journal] (
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [SeedName] [nvarchar](255) NOT NULL,
    [Hash] [nvarchar](64) NOT NULL,
    [Strategy] [nvarchar](50) NOT NULL,
    [ExecutedOn] [datetime2](7) NOT NULL,
    [Duration] [time](7) NOT NULL,
    CONSTRAINT [PK___seed_journal] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- Query seed execution history
SELECT 
    [SeedName],
    [Strategy], 
    [ExecutedOn],
    [Duration]
FROM [dbo].[__seed_journal] 
ORDER BY [ExecutedOn] DESC;
```

### SQL Server Performance Considerations

#### Batch Operations
```sql
-- Seeds/run-once/S004_SeedLargeDataset.sql
-- Use batch inserts for large datasets
INSERT INTO [Products] ([Name], [Category], [Price])
VALUES 
    ('Product 1', 'Electronics', 99.99),
    ('Product 2', 'Electronics', 149.99),
    -- ... batch of 1000 rows
    ('Product 1000', 'Books', 29.99);
```

#### Index Management
```sql
-- Seeds/run-once/S005_SeedWithIndexOptimization.sql
-- Disable indexes during large data loads
ALTER INDEX [IX_Products_Category] ON [Products] DISABLE;

-- Insert large dataset
INSERT INTO [Products] ([Name], [Category], [Price])
SELECT [Name], [Category], [Price] FROM [StagingProducts];

-- Rebuild indexes
ALTER INDEX [IX_Products_Category] ON [Products] REBUILD;
```

### Troubleshooting SQL Server Seeding

#### Seed Journal Issues
```sql
-- Check seed journal table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = '__seed_journal';

-- Manually reset a seed (for testing)
DELETE FROM [__seed_journal] 
WHERE [SeedName] = 'YourSeed.sql';
```

#### Permission Issues
```sql
-- Grant necessary permissions for seeding
GRANT INSERT, UPDATE, DELETE, SELECT ON [dbo].[__seed_journal] TO [DbReactorUser];
GRANT CREATE TABLE TO [DbReactorUser];  -- For journal table creation
```

#### Connection Issues
```csharp
// Use connection string with appropriate permissions
var connectionString = "Server=localhost;Database=MyApp;Integrated Security=true;MultipleActiveResultSets=true;";

// Or with SQL authentication
var connectionString = "Server=localhost;Database=MyApp;User Id=dbuser;Password=password;MultipleActiveResultSets=true;";
```

## Best Practices

1. **Use Transactions**: Each migration runs in its own transaction
2. **Test Migrations**: Always test migrations in a development environment first
3. **Backup Before Production**: Always backup your database before running production migrations
4. **Monitor Timeouts**: Adjust timeouts for long-running migrations
5. **Use Variables**: Make migrations environment-aware with variables
6. **Consistent Naming**: Use consistent migration naming for proper ordering

## Troubleshooting

### Scripts Not Found
```csharp
// Debug script discovery
var pendingMigrations = await engine.GetPendingUpgradesAsync();
Console.WriteLine($"Found {pendingMigrations.Count()} pending migrations");

// Or specify exact namespace
config.UseEmbeddedScriptsFromFolder(assembly, "YourApp.Scripts", "upgrades");
```

### Connection Issues
- Verify connection string format
- Check SQL Server is running and accessible
- Ensure database exists (or use `CreateDatabaseIfNotExists()`)
- Verify user permissions

### Timeout Issues
- Increase timeout for long-running migrations
- Consider breaking large migrations into smaller ones
- Monitor SQL Server performance during migrations

## Examples

See the [DbReactor.RunTest](../DbReactor.RunTest) project for a complete working example.

## Support

- **Core Documentation**: [DbReactor.Core README](../DbReactor.Core/README.md)
- **Issues**: [GitHub Issues](https://github.com/your-org/DbReactor/issues)
- **NuGet Package**: [DbReactor.MSSqlServer](https://www.nuget.org/packages/DbReactor.MSSqlServer)

## License

[Your License Here]