# DbReactor.Core - Database Migration Framework

DbReactor.Core is the foundational library for the DbReactor database migration framework. It provides database-agnostic interfaces, configuration management, and migration orchestration.

## Overview

DbReactor.Core contains the core abstractions and engine for database migrations but **does not include any database-specific implementations**. You'll need to install a database provider package (like `DbReactor.MSSqlServer`) to actually execute migrations.

## Installation

```bash
dotnet add package DbReactor.Core
```

**Note:** You'll also need a database provider package:
- `DbReactor.MSSqlServer` - For SQL Server support
- More providers coming soon...

## Quick Start

DbReactor.Core by itself cannot execute migrations. Here's a basic example using the SQL Server provider:

```csharp
// 1. Install both packages
// dotnet add package DbReactor.Core
// dotnet add package DbReactor.MSSqlServer

using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.MSSqlServer.Extensions;

// 2. Configure with a database provider
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)  // Requires DbReactor.MSSqlServer
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseConsoleLogging();

// 3. Run migrations
var engine = new DbReactorEngine(config);
var result = await engine.RunAsync();
```

## Core Concepts

### Configuration

`DbReactorConfiguration` is the central configuration object that holds all settings:

```csharp
var config = new DbReactorConfiguration()
    // Database provider (required - from extension package)
    .UseSqlServer(connectionString)
    
    // Script discovery (required)
    .UseStandardFolderStructure(assembly)
    .UseCodeScripts(assembly)
    
    // Optional features
    .UseConsoleLogging()
    .CreateDatabaseIfNotExists()
    .UseVariables(variables)
    .UseAscendingOrder();
```

### Migration Engine

The `DbReactorEngine` orchestrates migration execution:

```csharp
var engine = new DbReactorEngine(config);

// Preview migrations before execution (dry run)
var dryRunResult = await engine.RunPreviewAsync();
Console.WriteLine($"Would execute {dryRunResult.PendingMigrations} migrations");

// Execute migrations (async)
var result = await engine.RunAsync();

// Or use synchronous wrapper methods
var result = engine.Run();                    // Synchronous run
var upgradeResult = engine.ApplyUpgrades();   // Apply upgrades only
var downgradeResult = engine.ApplyDowngrades(); // Apply downgrades only
bool hasPending = engine.HasPendingUpgrades(); // Check for pending upgrades

// Check results
if (result.Successful)
{
    Console.WriteLine("Migrations completed successfully!");
}
else
{
    Console.WriteLine($"Migration failed: {result.ErrorMessage}");
}
```

### Script Discovery

DbReactor.Core provides several ways to discover migration scripts:

#### Standard Folder Structure
```csharp
.UseStandardFolderStructure(assembly)
// Looks for:
// - Scripts/upgrades/*.sql
// - Scripts/downgrades/*.sql
```

#### Custom Folder Structure
```csharp
.UseEmbeddedScriptsFromFolder(assembly, "MyApp.Migrations", "upgrades")
.UseEmbeddedScriptsFromFolder(assembly, "MyApp.Migrations", "downgrades")
```

#### Code Scripts (C# Classes)
```csharp
.UseCodeScripts(assembly)
// Looks for classes implementing ICodeScript
```

#### File System Scripts
```csharp
// Single directory
.UseFileSystemScripts("/path/to/scripts", ".sql", recursive: true)

// Standard folder structure on file system
.UseFileSystemFolderStructure("/path/to/base", "upgrades", "downgrades")

// File system downgrades
.UseFileSystemDowngrades("/path/to/downgrades")
```

### Migration Ordering

Control the order in which migrations are executed:

```csharp
.UseAscendingOrder()   // M001, M002, M003... (default)
.UseDescendingOrder()  // M003, M002, M001...
```

### Variable Substitution

Use variables in your migration scripts:

```csharp
.UseVariables(new Dictionary<string, string> {
    {"Environment", "Production"},
    {"TenantId", "12345"}
});
```

In your SQL files:
```sql
INSERT INTO Configuration (Environment, TenantId) 
VALUES ('${Environment}', '${TenantId}');
```

### Logging

Built-in logging providers:

```csharp
.UseConsoleLogging()           // Log to console
.AddLogProvider(new FileLogProvider("logs/db.log")) // Log to file  
.AddLogProvider(new CustomLogProvider()) // Custom logging
```

## Core Interfaces

### IConnectionManager
Manages database connections:
```csharp
public interface IConnectionManager
{
    Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
    Task ExecuteWithManagedConnectionAsync<T>(Func<IDbConnection, Task<T>> action, CancellationToken cancellationToken = default);
}
```

### IScriptExecutor
Executes migration scripts:
```csharp
public interface IScriptExecutor
{
    Task<MigrationResult> ExecuteAsync(IScript script, IConnectionManager connectionManager, CancellationToken cancellationToken = default);
}
```

### IMigrationJournal
Tracks executed migrations:
```csharp
public interface IMigrationJournal
{
    Task<bool> HasBeenExecutedAsync(IMigration migration, CancellationToken cancellationToken = default);
    Task StoreExecutedMigrationAsync(IMigration migration, MigrationResult result, CancellationToken cancellationToken = default);
    Task<IEnumerable<MigrationJournalEntry>> GetExecutedMigrationsAsync(CancellationToken cancellationToken = default);
}
```

### IDatabaseProvisioner
Creates databases if they don't exist:
```csharp
public interface IDatabaseProvisioner
{
    Task<bool> EnsureDatabaseExistsAsync(CancellationToken cancellationToken = default);
}
```

## Advanced Configuration

### Multiple Script Sources
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseEmbeddedScriptsFromFolder(coreAssembly, "Core.Scripts", "upgrades")
    .UseCodeScripts(migrationAssembly, "Migrations.Code")
    .UseEmbeddedScriptsFromFolder(moduleAssembly, "Module.Scripts", "migrations");
```

### Custom Script Discovery
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .AddScriptProvider(new MyCustomScriptProvider());
```

### Dry Run Mode (Preview)

Preview what migrations would be executed without actually running them:

```csharp
var engine = new DbReactorEngine(config);

// Preview migrations 
var dryRunResult = await engine.RunPreviewAsync();

// Access detailed information
Console.WriteLine($"Total migrations: {dryRunResult.TotalMigrations}");
Console.WriteLine($"Pending upgrades: {dryRunResult.PendingUpgrades}");
Console.WriteLine($"Pending downgrades: {dryRunResult.PendingDowngrades}");
Console.WriteLine($"Already executed: {dryRunResult.SkippedMigrations}");

// Output is also logged through the configured log provider
```

**Key Features:**
- Shows which migrations would be executed vs. skipped
- Detects both upgrade and downgrade operations
- Handles database existence checking
- Integrates with your logging configuration

### Downgrade Support
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(assembly)
    // Note: Downgrade support is enabled automatically when using downgrade resolvers
```

## File Structure

For embedded resources, mark SQL files in your `.csproj`:

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
V1_0_1_CreateUsersTable.sql
```

## Custom Extensibility

DbReactor.Core provides extension methods to plug in custom implementations of core interfaces, allowing you to customize any aspect of the migration process:

### Adding Custom Implementations

```csharp
var config = new DbReactorConfiguration()
    // Custom script discovery
    .AddScriptProvider(new MyCustomScriptProvider())
    .AddDowngradeResolver(new MyCustomDowngradeResolver())
    
    // Custom execution components
    .AddConnectionManager(new MyCustomConnectionManager())
    .AddScriptExecutor(new MyCustomScriptExecutor())
    .AddMigrationJournal(new MyCustomMigrationJournal())
    
    // Custom database management
    .AddDatabaseProvisioner(new MyCustomDatabaseProvisioner())
    
    // Custom logging
    .AddLogProvider(new MyCustomLogProvider());
```

### Available Extension Points

#### Script Discovery Extensions
- **`AddScriptProvider(IScriptProvider)`** - Add custom script discovery logic
- **`AddDowngradeResolver(IDowngradeResolver)`** - Add custom downgrade script resolution

#### Execution Extensions  
- **`AddConnectionManager(IConnectionManager)`** - Custom database connection management
- **`AddScriptExecutor(IScriptExecutor)`** - Custom script execution logic
- **`AddMigrationJournal(IMigrationJournal)`** - Custom migration tracking

#### Database Management Extensions
- **`AddDatabaseProvisioner(IDatabaseProvisioner)`** - Custom database creation logic

#### Logging Extensions
- **`AddLogProvider(ILogProvider)`** - Custom logging implementation

### Example Custom Implementations

#### Custom Script Provider
```csharp
public class ApiScriptProvider : IScriptProvider
{
    private readonly string _apiEndpoint;
    
    public ApiScriptProvider(string apiEndpoint)
    {
        _apiEndpoint = apiEndpoint;
    }
    
    public async Task<IEnumerable<IScript>> GetScriptsAsync(CancellationToken cancellationToken = default)
    {
        // Fetch scripts from remote API
        var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(_apiEndpoint);
        return ParseScriptsFromJson(response);
    }
}

// Usage
config.AddScriptProvider(new ApiScriptProvider("https://api.example.com/migrations"));
```

#### Custom Log Provider
```csharp
public class StructuredLogProvider : ILogProvider
{
    private readonly ILogger _logger;
    
    public StructuredLogProvider(ILogger logger)
    {
        _logger = logger;
    }
    
    public void WriteInformation(string format, params object[] args)
    {
        _logger.LogInformation(format, args);
    }
    
    public void WriteError(string format, params object[] args)
    {
        _logger.LogError(format, args);
    }
    
    public void WriteWarning(string format, params object[] args)
    {
        _logger.LogWarning(format, args);
    }
}

// Usage
config.AddLogProvider(new StructuredLogProvider(myLogger));
```

#### Custom Migration Journal
```csharp
public class RedisJournal : IMigrationJournal
{
    private readonly IDatabase _redis;
    
    public RedisJournal(IDatabase redis)
    {
        _redis = redis;
    }
    
    public async Task<bool> HasBeenExecutedAsync(IMigration migration, CancellationToken cancellationToken = default)
    {
        return await _redis.KeyExistsAsync($"migration:{migration.Name}");
    }
    
    public async Task StoreExecutedMigrationAsync(IMigration migration, MigrationResult result, CancellationToken cancellationToken = default)
    {
        await _redis.StringSetAsync($"migration:{migration.Name}", JsonSerializer.Serialize(result));
    }
    
    // ... implement other methods
}

// Usage
config.AddMigrationJournal(new RedisJournal(redisDatabase));
```

## Creating Database Providers

DbReactor.Core provides extension services to create custom database providers. Implement the core interfaces and use the provided services:

### Core Interfaces Required

```csharp
// Required interfaces for any database provider
public interface IConnectionManager
{
    Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
    Task ExecuteWithManagedConnectionAsync<T>(Func<IDbConnection, Task<T>> action, CancellationToken cancellationToken = default);
    // ... other connection management methods
}

public interface IScriptExecutor
{
    Task<MigrationResult> ExecuteAsync(IScript script, IConnectionManager connectionManager, CancellationToken cancellationToken = default);
    // ... other execution methods
}

public interface IMigrationJournal
{
    Task<bool> HasBeenExecutedAsync(IMigration migration, CancellationToken cancellationToken = default);
    Task StoreExecutedMigrationAsync(IMigration migration, MigrationResult result, CancellationToken cancellationToken = default);
    // ... other journaling methods
}

public interface IDatabaseProvisioner
{
    Task<bool> EnsureDatabaseExistsAsync(CancellationToken cancellationToken = default);
    // ... other provisioning methods
}
```

### Creating Extension Services

```csharp
public static class MyDatabaseExtensions
{
    public static DbReactorConfiguration UseMyDatabase(this DbReactorConfiguration config, string connectionString)
    {
        return config
            .AddConnectionManager(new MyConnectionManager(connectionString))
            .AddScriptExecutor(new MyScriptExecutor())
            .AddMigrationJournal(new MyMigrationJournal())
            .AddDatabaseProvisioner(new MyDatabaseProvisioner());
    }
    
    // Optional: Individual component configuration
    public static DbReactorConfiguration UseMyDatabaseConnection(this DbReactorConfiguration config, string connectionString)
    {
        return config.AddConnectionManager(new MyConnectionManager(connectionString));
    }
    
    public static DbReactorConfiguration UseMyDatabaseJournal(this DbReactorConfiguration config, string schema, string table)
    {
        return config.AddMigrationJournal(new MyMigrationJournal(schema, table));
    }
}
```

### Implementation Example

```csharp
public class MyConnectionManager : IConnectionManager
{
    private readonly string _connectionString;
    
    public MyConnectionManager(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MyDbConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
    
    public async Task ExecuteWithManagedConnectionAsync<T>(Func<IDbConnection, Task<T>> action, CancellationToken cancellationToken = default)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        return await action(connection);
    }
}

public class MyScriptExecutor : IScriptExecutor
{
    public async Task<MigrationResult> ExecuteAsync(IScript script, IConnectionManager connectionManager, CancellationToken cancellationToken = default)
    {
        return await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = script.Content;
            
            var stopwatch = Stopwatch.StartNew();
            await command.ExecuteNonQueryAsync(cancellationToken);
            stopwatch.Stop();
            
            return new MigrationResult
            {
                Successful = true,
                ExecutionTime = stopwatch.Elapsed
            };
        }, cancellationToken);
    }
}
```

### Available Services

DbReactor.Core provides the following services for extension creators:

- **Variable Substitution**: Use `IVariableSubstitutor` for ${Variable} replacement
- **Script Discovery**: Leverage `IScriptProvider` for custom script discovery
- **Migration Ordering**: Use `IMigrationOrderingStrategy` for custom ordering
- **Logging**: Implement `ILogProvider` for custom logging
- **Result Handling**: Use `MigrationResult` and `MigrationExecutionResult` for consistent results

### Testing Your Extension

```csharp
[Test]
public async Task MyDatabaseExtension_Should_Execute_Migrations()
{
    var config = new DbReactorConfiguration()
        .UseMyDatabase(connectionString)
        .UseStandardFolderStructure(typeof(MyTest).Assembly)
        .UseConsoleLogging();
    
    var engine = new DbReactorEngine(config);
    var result = await engine.RunAsync();
    
    Assert.That(result.Successful, Is.True);
}
```
```

## Database Provider Packages

### Available Providers
- **DbReactor.MSSqlServer** - SQL Server support with full feature set

### Planned Providers
- **DbReactor.PostgreSQL** - PostgreSQL support
- **DbReactor.MySQL** - MySQL support
- **DbReactor.SQLite** - SQLite support
- **DbReactor.Oracle** - Oracle support

### Community Providers
Create your own provider using the extension services documented above. Consider contributing back to the community!

## Support

- **Documentation**: See database provider packages for specific implementation details
- **Issues**: [GitHub Issues](https://github.com/your-org/DbReactor/issues)
- **Examples**: Check the provider package documentation for complete examples

## License

[Your License Here]