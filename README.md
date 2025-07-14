# DbReactor.Core - .NET Database Migration Framework

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![NuGet](https://img.shields.io/badge/nuget-v1.0.0-blue)]()
[![License](https://img.shields.io/badge/license-MIT-green)]()

DbReactor.Core is a powerful, extensible .NET database migration framework that provides version-controlled, repeatable database schema management. It supports multiple script types, database providers, and offers comprehensive tracking and rollback capabilities.

## 🚀 Features

- **Multiple Script Types**: SQL scripts, dynamic C# code scripts, and embedded resources
- **Database Agnostic**: Extensible architecture supports any database platform
- **Comprehensive Tracking**: Full migration history with execution time and rollback support
- **Safe Migrations**: Built-in SQL injection protection and transaction management
- **Flexible Discovery**: Multiple ways to organize and discover migration scripts
- **Async Support**: Non-blocking database operations with cancellation tokens
- **Robust Error Handling**: Detailed exception hierarchy with contextual information
- **Production Ready**: Enterprise-grade security and performance optimizations

## 📦 Installation

```bash
# Core framework
dotnet add package DbReactor.Core

# SQL Server provider
dotnet add package DbReactor.MSSqlServer
```

## 🏃 Quick Start

### Basic Setup

```csharp
using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.Core.Extensions;
using DbReactor.MSSqlServer.Extensions;

// Configure the migration engine with the new simplified API
var config = new DbReactorConfiguration()
    .UseSqlServer("Server=localhost;Database=MyApp;Trusted_Connection=true;")
    .UseConsoleLogging()
    .UseEmbeddedScripts(typeof(Program).Assembly);

// Create and run migrations
var engine = new DbReactorEngine(config);
var result = engine.Run();

if (result.Successful)
{
    Console.WriteLine("✅ Migration completed successfully!");
}
else
{
    Console.WriteLine($"❌ Migration failed: {result.ErrorMessage}");
}
```

### Project Structure

```
YourProject/
├── Scripts/
│   ├── upgrades/
│   │   ├── 001_CreateUsersTable.sql
│   │   ├── 002_SeedUsers.cs
│   │   └── 003_CreateIndexes.sql
│   └── downgrades/
│       ├── 001_CreateUsersTable.sql
│       ├── 002_SeedUsers.sql
│       └── 003_CreateIndexes.sql
└── Program.cs
```

Mark your SQL scripts as **Embedded Resources** in your project file:

```xml
<ItemGroup>
  <EmbeddedResource Include="Scripts\**\*.sql" />
</ItemGroup>
```

## 📚 Comprehensive User Guide

### Core Concepts

#### 1. Scripts
Scripts are the fundamental units of database changes. DbReactor supports three types:

**SQL Scripts** (`.sql` files):
```sql
-- 001_CreateUsersTable.sql
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
```

**Code Scripts** (C# classes):
```csharp
// 002_SeedUsers.cs
public class SeedUsers : ICodeScript
{
    public bool SupportsDowngrade => true;

    public string GetUpgradeScript(IConnectionManager connectionManager)
    {
        // Dynamic SQL generation with database queries
        var userCount = connectionManager.ExecuteWithManagedConnection(
            (conn, cmd) => {
                cmd.CommandText = "SELECT COUNT(*) FROM Users";
                return (int)cmd.ExecuteScalar();
            });

        if (userCount > 0) 
            return "-- Users already exist, skipping seed";
        
        return @"
            INSERT INTO Users (Username, Email) VALUES 
            ('admin', 'admin@company.com'),
            ('user1', 'user1@company.com')";
    }

    public string GetDowngradeScript(IConnectionManager connectionManager)
    {
        return "DELETE FROM Users WHERE Username IN ('admin', 'user1')";
    }
}
```

**Embedded Scripts**:
Scripts stored as embedded resources in your assembly, automatically discovered by namespace conventions.

#### 2. Migration Journal
DbReactor tracks executed migrations in a journal table (default: `MigrationJournal`):

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Auto-increment primary key |
| UpgradeScriptHash | NVARCHAR(256) | Unique hash of the upgrade script |
| MigrationName | NVARCHAR(512) | Human-readable migration name |
| DowngradeScript | NVARCHAR(MAX) | SQL for rollback (if available) |
| MigratedOn | DATETIME | When the migration was executed |
| ExecutionTime | TIME | How long the migration took |

#### 3. Configuration System
The `DbReactorConfiguration` class uses a simplified, user-friendly fluent API:

```csharp
var config = new DbReactorConfiguration()
    // Database provider with all SQL Server components
    .UseSqlServer(connectionString, commandTimeoutSeconds: 60)
    
    // Logging
    .UseConsoleLogging()
    // or .LogProvider = new CustomLogProvider()
    
    // Script discovery with convenient presets
    .UseStandardFolderStructure(assembly) // Uses Scripts/upgrades and Scripts/downgrades
    .UseCodeScripts(assembly)
    
    // Database management
    .CreateDatabaseIfNotExists()
    
    // Migration behavior
    .UseAscendingOrder() // or .UseDescendingOrder()
    
    // Variables (for script substitution)
    .UseVariables(new Dictionary<string, string> {
        {"Environment", "Production"},
        {"TenantId", "12345"}
    });
```

### Advanced Configuration

#### Multiple Script Sources
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseEmbeddedScriptsFromFolder(coreAssembly, "Core.Scripts", "upgrades")
    .UseCodeScripts(migrationAssembly, "Migrations.Code")
    .UseEmbeddedScriptsFromFolder(moduleAssembly, "Module.Scripts", "migrations");
```

#### Custom Downgrade Configuration
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseEmbeddedScriptsFromFolder(assembly, "Scripts", "upgrades")
    .UseDowngradesFromFolder(assembly, "Scripts", "downgrades");
```

#### Environment-Specific Configuration
```csharp
public static DbReactorConfiguration CreateConfiguration(string environment)
{
    var config = new DbReactorConfiguration()
        .UseSqlServer(GetConnectionString(environment))
        .UseConsoleLogging();

    if (environment == "Development")
    {
        config.CreateDatabaseIfNotExists()
              .UseCodeScripts(typeof(Program).Assembly);
    }
    else
    {
        config.UseEmbeddedScripts(typeof(Program).Assembly);
    }

    return config;
}
```

### Extension Methods Organization

DbReactor's extension methods are now organized into logical groups for better discoverability:

#### Database Provider Extensions
```csharp
using DbReactor.MSSqlServer.Extensions;

// Complete SQL Server setup with all components
config.UseSqlServer(connectionString, commandTimeoutSeconds: 60);

// Individual component configuration
config.UseSqlServerConnection(connectionString)
      .UseSqlServerExecutor(timeoutSeconds: 60)
      .UseSqlServerJournal(schema: "migrations", table: "journal")
      .UseSqlServerProvisioner(connectionString);
```

#### Script Discovery Extensions
```csharp
using DbReactor.Core.Extensions;

// Simple embedded script discovery
config.UseEmbeddedScripts(assembly);

// Standard folder structure (Scripts/upgrades, Scripts/downgrades)
config.UseStandardFolderStructure(assembly);

// Custom folder structure
config.UseEmbeddedScriptsFromFolder(assembly, "MyApp.Scripts", "migrations")
      .UseDowngradesFromFolder(assembly, "MyApp.Scripts", "rollbacks");

// Code script discovery
config.UseCodeScripts(assembly, targetNamespace: "MyApp.Migrations");
```

#### Migration Behavior Extensions
```csharp
// Execution order
config.UseAscendingOrder()     // or UseDescendingOrder()
      .UseCommandTimeout(120);

// Variables for script substitution
config.UseVariables(variables)
      .AddVariable("Environment", "Production");
```

#### Logging Extensions
```csharp
// Built-in console logging
config.UseConsoleLogging();

// Custom logging
config.LogProvider = new CustomLogProvider();
```

#### Database Management Extensions
```csharp
// Automatic database creation
config.CreateDatabaseIfNotExists(creationTemplate: "CREATE DATABASE {0}");
```

### Variable Substitution

DbReactor supports variable substitution in both SQL scripts and code scripts, allowing you to customize migrations based on environment, configuration, or runtime values.

#### Configuring Variables

```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseVariables(new Dictionary<string, string>
    {
        {"Environment", "Production"},
        {"DatabaseName", "MyApp_Prod"},
        {"AdminEmail", "admin@company.com"},
        {"TenantId", "12345"}
    })
    // Or add variables individually
    .AddVariable("BackupPath", @"C:\Backups\")
    .UseEmbeddedScripts(assembly);
```

#### SQL Script Variables

Use `${variableName}` syntax in SQL scripts for variable substitution:

```sql
-- 003_CreateEnvironmentSpecificTable.sql
CREATE TABLE ${Environment}_Logs (
    LogId INT PRIMARY KEY IDENTITY(1,1),
    Message NVARCHAR(MAX) NOT NULL,
    TenantId NVARCHAR(50) DEFAULT '${TenantId}',
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Create environment-specific index
CREATE INDEX IX_${Environment}_Logs_TenantId ON ${Environment}_Logs(TenantId);

-- Insert environment configuration
INSERT INTO Configuration (Key, Value) VALUES 
    ('Environment', '${Environment}'),
    ('AdminEmail', '${AdminEmail}'),
    ('BackupPath', '${BackupPath}');
```

#### Code Script Variables

Code scripts receive variables as a `IReadOnlyDictionary<string, string>` parameter:

```csharp
public class EnvironmentSpecificMigration : ICodeScript
{
    public bool SupportsDowngrade => true;

    public string GetUpgradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables)
    {
        // Access variables with safe defaults
        string environment = variables.TryGetValue("Environment", out string env) ? env : "Development";
        string tenantId = variables.TryGetValue("TenantId", out string tenant) ? tenant : "default";
        string adminEmail = variables.TryGetValue("AdminEmail", out string email) ? email : "admin@example.com";

        // Use variables in script generation
        var script = new StringBuilder();
        
        if (environment == "Production")
        {
            script.AppendLine($"-- Production environment setup for tenant {tenantId}");
            script.AppendLine($"INSERT INTO AdminUsers (Email, TenantId) VALUES ('{adminEmail}', '{tenantId}');");
            script.AppendLine("UPDATE Settings SET MaintenanceMode = 0;");
        }
        else
        {
            script.AppendLine($"-- Development environment setup for tenant {tenantId}");
            script.AppendLine($"INSERT INTO AdminUsers (Email, TenantId) VALUES ('{adminEmail}', '{tenantId}');");
            script.AppendLine("INSERT INTO TestData (Name) VALUES ('Sample Data');");
        }

        return script.ToString();
    }

    public string GetDowngradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables)
    {
        string tenantId = variables.TryGetValue("TenantId", out string tenant) ? tenant : "default";
        return $"DELETE FROM AdminUsers WHERE TenantId = '{tenantId}';";
    }

    // Backward compatibility - called when variables are not enabled
    public string GetUpgradeScript(IConnectionManager connectionManager)
    {
        return GetUpgradeScript(connectionManager, new Dictionary<string, string>());
    }

    public string GetDowngradeScript(IConnectionManager connectionManager)
    {
        return GetDowngradeScript(connectionManager, new Dictionary<string, string>());
    }
}
```

#### Advanced Variable Usage

```csharp
// Environment-specific configuration
var config = CreateConfiguration()
    .UseVariables(GetEnvironmentVariables(environment))
    .UseEmbeddedScripts(assembly);

private Dictionary<string, string> GetEnvironmentVariables(string environment)
{
    var variables = new Dictionary<string, string>
    {
        {"Environment", environment},
        {"DatabaseName", $"MyApp_{environment}"},
        {"LogLevel", environment == "Production" ? "Error" : "Debug"}
    };

    // Add environment-specific variables
    if (environment == "Production")
    {
        variables["BackupRetentionDays"] = "90";
        variables["MaxConnections"] = "100";
    }
    else
    {
        variables["BackupRetentionDays"] = "7";
        variables["MaxConnections"] = "10";
    }

    return variables;
}
```

#### Variable Best Practices

1. **Always provide defaults** in code scripts to handle missing variables gracefully
2. **Use descriptive variable names** that clearly indicate their purpose
3. **Validate critical variables** in code scripts before using them
4. **Keep variable values simple** - avoid complex JSON or special characters in SQL substitution
5. **Document required variables** for each migration that uses them

```csharp
// Example of robust variable handling
public string GetUpgradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables)
{
    // Validate required variables
    if (!variables.TryGetValue("TenantId", out string tenantId) || string.IsNullOrEmpty(tenantId))
    {
        throw new ArgumentException("TenantId variable is required for this migration");
    }

    // Use optional variables with defaults
    string environment = variables.TryGetValue("Environment", out string env) ? env : "Development";
    int maxRetries = int.TryParse(variables.TryGetValue("MaxRetries", out string retries) ? retries : "3", out int result) ? result : 3;

    return $@"
        INSERT INTO TenantConfiguration (TenantId, Environment, MaxRetries) 
        VALUES ('{tenantId}', '{environment}', {maxRetries});
    ";
}
```

### Migration Patterns

#### 1. Basic Schema Migration
```sql
-- 001_CreateTables.sql
CREATE TABLE Products (
    ProductId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE INDEX IX_Products_Name ON Products(Name);
```

#### 2. Data Migration with Rollback
```csharp
public class MigrateUserRoles : ICodeScript
{
    public bool SupportsDowngrade => true;

    public string GetUpgradeScript(IConnectionManager connectionManager)
    {
        return @"
            -- Backup existing data
            SELECT * INTO Users_Backup_003 FROM Users;
            
            -- Add new column
            ALTER TABLE Users ADD RoleId INT;
            
            -- Migrate data
            UPDATE Users SET RoleId = 1 WHERE Username IN ('admin', 'superuser');
            UPDATE Users SET RoleId = 2 WHERE RoleId IS NULL;
        ";
    }

    public string GetDowngradeScript(IConnectionManager connectionManager)
    {
        return @"
            -- Remove the column
            ALTER TABLE Users DROP COLUMN RoleId;
            
            -- Clean up backup
            DROP TABLE Users_Backup_003;
        ";
    }
}
```

#### 3. Conditional Migrations
```csharp
public class ConditionalIndexCreation : ICodeScript
{
    public bool SupportsDowngrade => false;

    public string GetUpgradeScript(IConnectionManager connectionManager)
    {
        var sb = new StringBuilder();
        
        // Check if index already exists
        var indexExists = connectionManager.ExecuteWithManagedConnection(
            (conn, cmd) => {
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM sys.indexes 
                    WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('Users')";
                return (int)cmd.ExecuteScalar() > 0;
            });

        if (!indexExists)
        {
            sb.AppendLine("CREATE INDEX IX_Users_Email ON Users(Email);");
        }

        // Add more conditional logic
        var recordCount = connectionManager.ExecuteWithManagedConnection(
            (conn, cmd) => {
                cmd.CommandText = "SELECT COUNT(*) FROM Users";
                return (int)cmd.ExecuteScalar();
            });

        if (recordCount > 10000)
        {
            sb.AppendLine("CREATE INDEX IX_Users_CreatedAt ON Users(CreatedAt);");
        }

        return sb.ToString();
    }

    public string GetDowngradeScript(IConnectionManager connectionManager)
    {
        throw new NotSupportedException("This migration cannot be rolled back");
    }
}
```

### Error Handling and Debugging

#### Exception Types
DbReactor provides specific exception types for different failure scenarios:

- `ConfigurationException`: Invalid configuration settings
- `MigrationExecutionException`: Script execution failures
- `ScriptDiscoveryException`: Problems finding scripts
- `JournalException`: Migration tracking issues
- `DatabaseConnectionException`: Connection problems

#### Debugging Failed Migrations
```csharp
try
{
    var result = engine.Run();
    if (!result.Successful)
    {
        Console.WriteLine($"Migration failed: {result.ErrorMessage}");
        
        // Check individual script results
        foreach (var scriptResult in result.Scripts.Where(s => !s.Successful))
        {
            Console.WriteLine($"Failed script: {scriptResult.Script.Name}");
            Console.WriteLine($"Error: {scriptResult.ErrorMessage}");
            Console.WriteLine($"Execution time: {scriptResult.ExecutionTime}");
        }
    }
}
catch (MigrationExecutionException ex)
{
    Console.WriteLine($"Script '{ex.ScriptName}' failed during {ex.Operation}");
    Console.WriteLine($"Details: {ex.Message}");
    Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
}
```

### Best Practices

#### 1. Script Naming and Organization
```
Scripts/
├── upgrades/
│   ├── 2025/
│   │   ├── 01-january/
│   │   │   ├── 001_AddUserTable.sql
│   │   │   ├── 002_SeedInitialData.cs
│   │   │   └── 003_CreateIndexes.sql
│   │   └── 02-february/
│   │       └── 004_AddProductTable.sql
│   └── archive/
│       └── older-migrations/
└── downgrades/
    ├── 001_AddUserTable.sql
    ├── 002_SeedInitialData.sql
    └── 003_CreateIndexes.sql
```

#### 2. Safe SQL Practices
```sql
-- ✅ Good: Idempotent operations
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (UserId INT PRIMARY KEY);
END

-- ✅ Good: Check before dropping
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OldIndex')
BEGIN
    DROP INDEX IX_OldIndex ON Users;
END

-- ❌ Avoid: Non-idempotent operations
CREATE TABLE Users (UserId INT); -- Will fail if table exists
```

#### 3. Code Script Guidelines
```csharp
public class GoodMigrationExample : ICodeScript
{
    public bool SupportsDowngrade => true;

    public string GetUpgradeScript(IConnectionManager connectionManager)
    {
        // ✅ Good: Validate preconditions
        var tableExists = connectionManager.ExecuteWithManagedConnection(
            (conn, cmd) => {
                cmd.CommandText = "SELECT OBJECT_ID('Users')";
                return cmd.ExecuteScalar() != null;
            });

        if (!tableExists)
        {
            throw new InvalidOperationException("Users table must exist before running this migration");
        }

        // ✅ Good: Use StringBuilder for complex SQL
        var sql = new StringBuilder();
        sql.AppendLine("-- Migration: Add user preferences");
        sql.AppendLine("ALTER TABLE Users ADD Preferences NVARCHAR(MAX);");
        
        // ✅ Good: Provide meaningful comments
        sql.AppendLine("-- Set default preferences for existing users");
        sql.AppendLine("UPDATE Users SET Preferences = '{}' WHERE Preferences IS NULL;");

        return sql.ToString();
    }

    public string GetDowngradeScript(IConnectionManager connectionManager)
    {
        // ✅ Good: Provide complete rollback
        return @"
            -- Remove preferences column
            ALTER TABLE Users DROP COLUMN Preferences;
        ";
    }
}
```

## 🔧 Creating Custom Providers and Extensions

### Custom Script Provider

Create a provider that loads scripts from external sources:

```csharp
using DbReactor.Core.Discovery;
using DbReactor.Core.Abstractions;
using DbReactor.Core.Models.Scripts;

public class HttpScriptProvider : IScriptProvider
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;

    public HttpScriptProvider(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
    }

    public IEnumerable<IScript> GetScripts()
    {
        // Fetch script list from API
        var scriptList = FetchScriptListFromApi();
        
        foreach (var scriptInfo in scriptList)
        {
            var scriptContent = FetchScriptContent(scriptInfo.Url);
            yield return new GenericScript(scriptInfo.Name, scriptContent);
        }
    }

    private ScriptInfo[] FetchScriptListFromApi()
    {
        var response = _httpClient.GetStringAsync($"{_baseUrl}/scripts").Result;
        return JsonSerializer.Deserialize<ScriptInfo[]>(response);
    }

    private string FetchScriptContent(string url)
    {
        return _httpClient.GetStringAsync(url).Result;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Usage
var config = new DbReactorConfiguration()
    .WithSqlServer(connectionString)
    .AddScriptProvider(new HttpScriptProvider("https://api.company.com/migrations"));
```

### Custom Database Provider

Create a complete database provider for PostgreSQL:

```csharp
// 1. Connection Manager
using Npgsql;

public class PostgresConnectionManager : IConnectionManager
{
    private readonly string _connectionString;

    public PostgresConnectionManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public void ExecuteCommandsWithManagedConnection(Action<Func<IDbCommand>> action)
    {
        using var connection = CreateConnection();
        connection.Open();
        action(() => connection.CreateCommand());
    }

    public T ExecuteCommandsWithManagedConnection<T>(Func<Func<IDbCommand>, T> action)
    {
        using var connection = CreateConnection();
        connection.Open();
        return action(() => connection.CreateCommand());
    }

    public void ExecuteWithManagedConnection(Action<IDbConnection, IDbCommand> operation)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        operation(connection, command);
    }

    public T ExecuteWithManagedConnection<T>(Func<IDbConnection, IDbCommand, T> operation)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        return operation(connection, command);
    }
}

// 2. Script Executor
public class PostgresScriptExecutor : IScriptExecutor
{
    public MigrationResult Execute(IScript script, IConnectionManager connectionManager)
    {
        var result = new MigrationResult { Script = script };
        var startTime = DateTime.UtcNow;

        try
        {
            connectionManager.ExecuteWithManagedConnection((connection, command) =>
            {
                // Split on PostgreSQL-specific separators
                var batches = SplitIntoBatches(script.Script);
                
                foreach (var batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    
                    command.CommandText = batch;
                    command.ExecuteNonQuery();
                }
            });

            result.Successful = true;
        }
        catch (Exception ex)
        {
            result.Successful = false;
            result.Error = ex;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            result.ExecutionTime = DateTime.UtcNow - startTime;
        }

        return result;
    }

    private string[] SplitIntoBatches(string script)
    {
        // PostgreSQL doesn't use GO like SQL Server
        // Split on semicolons outside of quoted strings
        return SplitSqlScript(script);
    }

    private string[] SplitSqlScript(string script)
    {
        // Implementation for splitting PostgreSQL scripts
        // Handle quoted strings, comments, etc.
        return script.Split(';', StringSplitOptions.RemoveEmptyEntries);
    }
}

// 3. Migration Journal
public class PostgresMigrationJournal : IMigrationJournal
{
    private readonly string _tableName;
    private readonly string _schemaName;

    public PostgresMigrationJournal(string schemaName = "public", string tableName = "migration_journal")
    {
        _schemaName = schemaName;
        _tableName = tableName;
    }

    public void EnsureTableExists(IConnectionManager connectionManager)
    {
        connectionManager.ExecuteWithManagedConnection((connection, command) =>
        {
            // Check if table exists
            command.CommandText = $@"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = '{_schemaName}' 
                    AND table_name = '{_tableName}'
                )";

            var exists = (bool)command.ExecuteScalar();
            
            if (!exists)
            {
                // Create table with PostgreSQL syntax
                command.CommandText = $@"
                    CREATE TABLE {_schemaName}.{_tableName} (
                        id SERIAL PRIMARY KEY,
                        upgrade_script_hash VARCHAR(256) NOT NULL,
                        migration_name VARCHAR(512) NOT NULL,
                        downgrade_script TEXT,
                        migrated_on TIMESTAMP NOT NULL DEFAULT NOW(),
                        execution_time INTERVAL NOT NULL
                    )";
                command.ExecuteNonQuery();

                // Create index
                command.CommandText = $@"
                    CREATE UNIQUE INDEX ix_{_tableName}_hash 
                    ON {_schemaName}.{_tableName}(upgrade_script_hash)";
                command.ExecuteNonQuery();
            }
        });
    }

    public void StoreExecutedMigration(IMigration migration, MigrationResult result)
    {
        // Implementation for storing migration in PostgreSQL
        // Use parameterized queries to prevent SQL injection
    }

    public void RemoveExecutedMigration(string upgradeScriptHash)
    {
        // Implementation for removing migration from PostgreSQL
    }

    public IEnumerable<MigrationJournalEntry> GetExecutedMigrations()
    {
        // Implementation for reading migrations from PostgreSQL
        return new List<MigrationJournalEntry>();
    }

    public bool HasBeenExecuted(IMigration migration)
    {
        // Implementation for checking if migration was executed
        return false;
    }
}

// 4. Extension Methods for Fluent Configuration
public static class PostgresExtensions
{
    public static DbReactorConfiguration WithPostgreSQL(
        this DbReactorConfiguration config,
        string connectionString,
        int commandTimeout = 30)
    {
        var connectionManager = new PostgresConnectionManager(connectionString);
        var scriptExecutor = new PostgresScriptExecutor();
        var journal = new PostgresMigrationJournal();

        config.ConnectionManager = connectionManager;
        config.ScriptExecutor = scriptExecutor;
        config.ScriptJournal = journal;
        config.CommandTimeout = commandTimeout;

        return config;
    }

    public static DbReactorConfiguration WithPostgresJournal(
        this DbReactorConfiguration config,
        string schemaName = "public",
        string tableName = "migration_journal")
    {
        config.ScriptJournal = new PostgresMigrationJournal(schemaName, tableName);
        return config;
    }
}

// Usage
var config = new DbReactorConfiguration()
    .WithPostgreSQL("Host=localhost;Database=myapp;Username=user;Password=pass")
    .WithLogToConsole()
    .WithEmbeddedScripts(typeof(Program).Assembly);
```

### Custom Migration Journal

Create a journal that stores migration history in JSON files:

```csharp
using System.Text.Json;

public class JsonMigrationJournal : IMigrationJournal
{
    private readonly string _journalFilePath;
    private readonly object _fileLock = new object();

    public JsonMigrationJournal(string journalFilePath = "migration-journal.json")
    {
        _journalFilePath = journalFilePath;
    }

    public void EnsureTableExists(IConnectionManager connectionManager)
    {
        // Ensure the JSON file exists
        if (!File.Exists(_journalFilePath))
        {
            lock (_fileLock)
            {
                if (!File.Exists(_journalFilePath))
                {
                    File.WriteAllText(_journalFilePath, "[]");
                }
            }
        }
    }

    public void StoreExecutedMigration(IMigration migration, MigrationResult result)
    {
        lock (_fileLock)
        {
            var entries = LoadEntries();
            
            var newEntry = new MigrationJournalEntry
            {
                Id = entries.Count + 1,
                UpgradeScriptHash = migration.UpgradeScript.Hash,
                MigrationName = migration.Name,
                DowngradeScript = migration.DowngradeScriptContent,
                MigratedOn = DateTime.UtcNow,
                ExecutionTime = result.ExecutionTime
            };

            entries.Add(newEntry);
            SaveEntries(entries);
        }
    }

    public void RemoveExecutedMigration(string upgradeScriptHash)
    {
        lock (_fileLock)
        {
            var entries = LoadEntries();
            entries.RemoveAll(e => e.UpgradeScriptHash == upgradeScriptHash);
            SaveEntries(entries);
        }
    }

    public IEnumerable<MigrationJournalEntry> GetExecutedMigrations()
    {
        return LoadEntries();
    }

    public bool HasBeenExecuted(IMigration migration)
    {
        var entries = LoadEntries();
        return entries.Any(e => e.UpgradeScriptHash == migration.UpgradeScript.Hash);
    }

    private List<MigrationJournalEntry> LoadEntries()
    {
        if (!File.Exists(_journalFilePath))
            return new List<MigrationJournalEntry>();

        var json = File.ReadAllText(_journalFilePath);
        return JsonSerializer.Deserialize<List<MigrationJournalEntry>>(json) 
               ?? new List<MigrationJournalEntry>();
    }

    private void SaveEntries(List<MigrationJournalEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        File.WriteAllText(_journalFilePath, json);
    }
}

// Usage
var config = new DbReactorConfiguration()
    .WithSqlServer(connectionString)
    .WithScriptJournal(new JsonMigrationJournal("./migrations.json"))
    .WithEmbeddedScripts(typeof(Program).Assembly);
```

### Custom Downgrade Resolver

Create a resolver that finds downgrade scripts using Git history:

```csharp
using LibGit2Sharp;

public class GitBasedDowngradeResolver : IDowngradeResolver
{
    private readonly string _repositoryPath;
    private readonly string _upgradesPath;

    public GitBasedDowngradeResolver(string repositoryPath, string upgradesPath = "Scripts/upgrades")
    {
        _repositoryPath = repositoryPath;
        _upgradesPath = upgradesPath;
    }

    public IScript FindDowngradeScript(IScript upgradeScript)
    {
        using var repo = new Repository(_repositoryPath);
        
        // Find the commit that introduced this upgrade script
        var scriptPath = Path.Combine(_upgradesPath, upgradeScript.Name + ".sql");
        var commits = repo.Commits.QueryBy(scriptPath);
        
        var introducingCommit = commits.FirstOrDefault();
        if (introducingCommit == null)
        {
            return null; // No downgrade available
        }

        // Look for a corresponding downgrade script in the same commit
        var downgradeScriptPath = scriptPath.Replace("upgrades", "downgrades");
        
        try
        {
            var blob = introducingCommit.Commit[downgradeScriptPath]?.Target as Blob;
            if (blob != null)
            {
                var content = blob.GetContentText();
                return new GenericScript(upgradeScript.Name + "_downgrade", content);
            }
        }
        catch
        {
            // Downgrade script not found in this commit
        }

        return null;
    }

    public IEnumerable<IScript> GetDowngradeScripts()
    {
        // Return all downgrade scripts found in the repository
        using var repo = new Repository(_repositoryPath);
        var downgradesPath = _upgradesPath.Replace("upgrades", "downgrades");
        
        var tree = repo.Head.Tip.Tree;
        var downgradeFolder = tree[downgradesPath]?.Target as Tree;
        
        if (downgradeFolder == null)
            yield break;

        foreach (var item in downgradeFolder.Where(i => i.Name.EndsWith(".sql")))
        {
            var blob = item.Target as Blob;
            if (blob != null)
            {
                var content = blob.GetContentText();
                var name = Path.GetFileNameWithoutExtension(item.Name);
                yield return new GenericScript(name, content);
            }
        }
    }
}

// Usage
var config = new DbReactorConfiguration()
    .WithSqlServer(connectionString)
    .WithEmbeddedScripts(typeof(Program).Assembly)
    .WithDowngradeResolver(new GitBasedDowngradeResolver("./"))
    .WithDowngrades();
```

### Custom Log Provider

Create a structured logging provider:

```csharp
using Microsoft.Extensions.Logging;

public class StructuredLogProvider : ILogProvider
{
    private readonly ILogger<StructuredLogProvider> _logger;

    public StructuredLogProvider(ILogger<StructuredLogProvider> logger)
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

// Or create a more sophisticated provider with structured data
public class TelemetryLogProvider : ILogProvider
{
    private readonly TelemetryClient _telemetryClient;

    public TelemetryLogProvider(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public void WriteInformation(string format, params object[] args)
    {
        var message = string.Format(format, args);
        _telemetryClient.TrackTrace(message, SeverityLevel.Information);
    }

    public void WriteError(string format, params object[] args)
    {
        var message = string.Format(format, args);
        _telemetryClient.TrackTrace(message, SeverityLevel.Error);
        
        // Also track as exception if this is an error
        _telemetryClient.TrackException(new DbReactorException(message));
    }

    public void WriteWarning(string format, params object[] args)
    {
        var message = string.Format(format, args);
        _telemetryClient.TrackTrace(message, SeverityLevel.Warning);
    }
}
```

### Extension Guidelines

When creating custom providers and extensions:

#### 1. Interface Implementation
- Always implement required interfaces completely
- Handle null inputs gracefully
- Provide meaningful error messages
- Use proper resource disposal (`IDisposable`)

#### 2. Configuration Integration
```csharp
public static class CustomExtensions
{
    public static DbReactorConfiguration WithCustomProvider(
        this DbReactorConfiguration config,
        CustomProviderOptions options)
    {
        // Validate options
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // Create and configure provider
        var provider = new CustomProvider(options);
        
        // Add to configuration
        config.AddScriptProvider(provider);
        
        return config;
    }
}
```

#### 3. Error Handling
```csharp
public class CustomProvider : IScriptProvider
{
    public IEnumerable<IScript> GetScripts()
    {
        try
        {
            // Your implementation
            return DiscoverScripts();
        }
        catch (Exception ex)
        {
            throw new ScriptDiscoveryException(
                $"Failed to discover scripts from {GetType().Name}: {ex.Message}", 
                ex);
        }
    }
}
```

#### 4. Async Support
If creating async providers, implement both sync and async interfaces:

```csharp
public class AsyncCustomProvider : IScriptProvider, IScriptProviderAsync
{
    // Sync implementation
    public IEnumerable<IScript> GetScripts()
    {
        return GetScriptsAsync().GetAwaiter().GetResult();
    }

    // Async implementation
    public async Task<IEnumerable<IScript>> GetScriptsAsync(CancellationToken cancellationToken = default)
    {
        // Your async implementation
        return await DiscoverScriptsAsync(cancellationToken);
    }
}
```

## 🧪 Testing

### Unit Testing Migration Scripts

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DbReactor.Core.Models.Scripts;

[TestClass]
public class MigrationTests
{
    [TestMethod]
    public void SeedUsers_GeneratesCorrectUpgradeScript()
    {
        // Arrange
        var migration = new _002_SeedUsers();
        var mockConnectionManager = new Mock<IConnectionManager>();
        
        // Act
        var script = migration.GetUpgradeScript(mockConnectionManager.Object);
        
        // Assert
        Assert.IsTrue(script.Contains("INSERT INTO Users"));
        Assert.IsTrue(script.Contains("IF NOT EXISTS"));
    }

    [TestMethod]
    public void SeedUsers_GeneratesCorrectDowngradeScript()
    {
        // Arrange
        var migration = new _002_SeedUsers();
        var mockConnectionManager = new Mock<IConnectionManager>();
        
        // Act
        var script = migration.GetDowngradeScript(mockConnectionManager.Object);
        
        // Assert
        Assert.IsTrue(script.Contains("DELETE FROM Users"));
        Assert.IsTrue(script.Contains("WHERE Username IN"));
    }
}
```

### Integration Testing

```csharp
[TestClass]
public class IntegrationTests
{
    private const string TestConnectionString = 
        "Server=localhost;Database=DbReactor_Test;Trusted_Connection=true;";

    [TestMethod]
    public void CompleteFlow_UpgradeAndDowngrade_Success()
    {
        // Arrange
        var config = new DbReactorConfiguration()
            .WithSqlServer(TestConnectionString)
            .WithEnsureDatabaseExists()
            .WithEmbeddedScripts(typeof(IntegrationTests).Assembly)
            .WithDowngrades();

        var engine = new DbReactorEngine(config);

        try
        {
            // Act - Run upgrades
            var upgradeResult = engine.ApplyUpgrades();
            
            // Assert - Upgrades successful
            Assert.IsTrue(upgradeResult.Successful);
            
            // Act - Run downgrades
            var downgradeResult = engine.ApplyDowngrades();
            
            // Assert - Downgrades successful
            Assert.IsTrue(downgradeResult.Successful);
        }
        finally
        {
            // Cleanup test database
            CleanupTestDatabase();
        }
    }
}
```

## 🚀 Performance Optimization

### Best Practices for Large Databases

#### 1. Batch Operations
```csharp
public class LargeDataMigration : ICodeScript
{
    public string Name => "010_MigrateLargeDataset";
    public bool SupportsDowngrade => false;

    public string GetUpgradeScript(IConnectionManager connectionManager)
    {
        return @"
            -- Process in batches to avoid lock escalation
            DECLARE @BatchSize INT = 10000;
            DECLARE @RowsProcessed INT = 0;
            
            WHILE (1 = 1)
            BEGIN
                UPDATE TOP (@BatchSize) LargeTable
                SET NewColumn = 'DefaultValue'
                WHERE NewColumn IS NULL;
                
                SET @RowsProcessed = @@ROWCOUNT;
                
                IF @RowsProcessed = 0 BREAK;
                
                -- Brief pause to allow other operations
                WAITFOR DELAY '00:00:01';
            END
        ";
    }
}
```

#### 2. Index Management
```csharp
public class IndexOptimization : ICodeScript
{
    public string Name => "011_OptimizeIndexes";
    public bool SupportsDowngrade => true;

    public string GetUpgradeScript(IConnectionManager connectionManager)
    {
        return @"
            -- Drop indexes before large data operations
            IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LargeTable_OldColumn')
                DROP INDEX IX_LargeTable_OldColumn ON LargeTable;
            
            -- Perform data operations
            ALTER TABLE LargeTable ADD NewIndexColumn AS (UPPER(Name)) PERSISTED;
            
            -- Recreate optimized indexes
            CREATE INDEX IX_LargeTable_NewIndexColumn ON LargeTable(NewIndexColumn)
            WITH (ONLINE = ON, MAXDOP = 4);
        ";
    }

    public string GetDowngradeScript(IConnectionManager connectionManager)
    {
        return @"
            DROP INDEX IF EXISTS IX_LargeTable_NewIndexColumn ON LargeTable;
            ALTER TABLE LargeTable DROP COLUMN NewIndexColumn;
            
            -- Recreate original index
            CREATE INDEX IX_LargeTable_OldColumn ON LargeTable(Name);
        ";
    }
}
```

## 📖 API Reference

### Core Classes

#### DbReactorEngine
Main entry point for migration operations.

**Methods:**
- `DbReactorResult Run()` - Execute full migration process
- `DbReactorResult ApplyUpgrades()` - Apply pending upgrades only
- `DbReactorResult ApplyDowngrades()` - Apply downgrades for removed migrations
- `bool HasPendingUpgrades()` - Check if upgrades are needed
- `IEnumerable<IMigration> GetPendingUpgrades()` - Get list of pending migrations
- `IEnumerable<IMigration> GetAppliedUpgrades()` - Get list of applied migrations

#### DbReactorConfiguration
Fluent configuration builder.

**Key Methods:**
- `WithSqlServer(string connectionString, int timeout = 30)`
- `WithLogToConsole()` / `WithLogProvider(ILogProvider provider)`
- `WithEmbeddedScripts(Assembly assembly)`
- `WithCodeScripts(Assembly assembly)`
- `AddScriptProvider(IScriptProvider provider)`
- `WithDowngrades()` / `WithDowngradeResolver(IDowngradeResolver resolver)`
- `WithEnsureDatabaseExists()`
- `WithScriptExecutionOrder(ScriptExecutionOrder order)`

### Interfaces

#### ICodeScript
Interface for dynamic C# migration scripts.

**Properties:**
- `string Name { get; }` - Unique name for the script
- `bool SupportsDowngrade { get; }` - Whether downgrade is available

**Methods:**
- `string GetUpgradeScript(IConnectionManager connectionManager)` - Generate upgrade SQL
- `string GetDowngradeScript(IConnectionManager connectionManager)` - Generate downgrade SQL

#### IScriptProvider
Interface for discovering migration scripts.

**Methods:**
- `IEnumerable<IScript> GetScripts()` - Return all discovered scripts

#### IMigrationJournal
Interface for tracking migration execution.

**Methods:**
- `void EnsureTableExists(IConnectionManager connectionManager)` - Create journal table
- `void StoreExecutedMigration(IMigration migration, MigrationResult result)` - Record execution
- `void RemoveExecutedMigration(string upgradeScriptHash)` - Remove from history
- `IEnumerable<MigrationJournalEntry> GetExecutedMigrations()` - Get execution history
- `bool HasBeenExecuted(IMigration migration)` - Check if migration was run

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Setup
1. Clone the repository
2. Install .NET 8 SDK
3. Run `dotnet restore`
4. Run `dotnet build`
5. Run tests with `dotnet test`

### Creating a Database Provider
1. Implement required interfaces (`IConnectionManager`, `IScriptExecutor`, `IMigrationJournal`)
2. Create extension methods for fluent configuration
3. Add comprehensive tests
4. Update documentation

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📚 [Documentation](docs/)
- 🐛 [Issue Tracker](https://github.com/your-org/dbreactor/issues)
- 💬 [Discussions](https://github.com/your-org/dbreactor/discussions)
- 📧 [Email Support](mailto:support@dbreactor.com)

## 🔄 Version History

### v1.0.0
- Initial release
- SQL Server provider
- Embedded and Code script support
- Migration journal tracking
- Comprehensive error handling

---

**Built with ❤️ for the .NET community**