# DbReactor.Core - .NET Database Migration Framework


DbReactor.Core is a powerful, extensible .NET database migration framework that provides version-controlled, repeatable database schema management. It supports multiple script types, database providers, and offers comprehensive tracking and rollback capabilities.

## Table of Contents

- [Quick Start](#-quick-start) - Get running in 5 minutes
- [Features](#features) - Key capabilities
- [Installation](#installation) - Package setup
- [Quick Example](#quick-example) - Basic usage
- [Comprehensive User Guide](#comprehensive-user-guide) - Complete documentation
- [Script Discovery & Ordering](#script-discovery-and-ordering) - How migrations are found
- [Migration Patterns](#migration-patterns) - Common scenarios
- [Best Practices](#best-practices) - Naming strategies and tips
- [API Reference](#api-reference) - Method documentation
- [Support](#support) - Help and resources

## Features

- **Multiple Script Types**: SQL scripts, dynamic C# code scripts, and embedded resources
- **Database Agnostic**: Extensible architecture supports any database platform
- **Comprehensive Tracking**: Full migration history with execution time and rollback support
- **Safe Migrations**: Built-in SQL injection protection and transaction management
- **Flexible Discovery**: Multiple ways to organize and discover migration scripts
- **Async-First Architecture**: Non-blocking database operations with cancellation tokens and sync wrappers (including DatabaseProvisioner)
- **Robust Error Handling**: Detailed exception hierarchy with contextual information
- **Production Ready**: Enterprise-grade security and performance optimizations

## Quick Start

**New to DbReactor?** Check out the [Quick Start Guide](QUICKSTART.md) to get running in 5 minutes!

## Installation

```bash
# Core framework
dotnet add package DbReactor.Core

# SQL Server provider
dotnet add package DbReactor.MSSqlServer
```

## Quick Example

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

// Synchronous execution (recommended for console apps)
var result = engine.Run();

if (result.Successful)
{
    Console.WriteLine("Migration completed successfully!");
}
else
{
    Console.WriteLine($"Migration failed: {result.ErrorMessage}");
}

// Or run asynchronously
var asyncResult = await engine.RunAsync();
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

## Comprehensive User Guide

### Core Concepts

#### 1. Scripts
Scripts are the fundamental units of database changes. DbReactor supports three types:

##### **Script Discovery and Ordering**

DbReactor automatically discovers and orders scripts using the following rules:

**Discovery Process:**
1. **Embedded SQL Scripts**: Found as embedded resources in your assembly
2. **Code Scripts**: Found as C# classes implementing `ICodeScript`
3. **Namespace Detection**: Automatically detects base namespace from embedded resources
4. **Alphabetical Sorting**: All scripts are sorted alphabetically by their full name

**Sorting Behavior:**
- Scripts are sorted by their complete name (including namespace)
- Mixed file types (SQL + C#) are sorted together
- Case-sensitive alphabetical order is used
- Example order: `M001_CreateTable.sql`, `M002_SeedData.cs`, `M003_AddIndexes.sql`

**Recommended Naming Strategy:**
While you can use any naming convention, we recommend:
```
M001_CreateUsersTable.sql
M002_SeedUsers.cs  
M003_CreateProductsTable.sql
M004_CreateOrdersTable.sql
M005_EnvironmentSpecificSettings.sql
```

**Why This Works:**
- ✅ **Consistent sorting** across all database types and file extensions
- ✅ **C# class compatibility** - `M001_CreateTable` is a valid C# class name
- ✅ **Self-documenting** - clear sequence and purpose
- ✅ **Database agnostic** - works with SQL Server, PostgreSQL, MySQL, MongoDB, etc.
- ✅ **File type agnostic** - works with .sql, .cs, .json, .xml, .py, .js, etc.

**Alternative Strategies:**
```
# Date-based (good for teams)
20250716_001_CreateUsersTable.sql
20250716_002_SeedUsers.cs

# Semantic versioning
V1_0_1_CreateUsersTable.sql
V1_0_2_SeedUsers.cs

# Simple sequential
001_CreateUsersTable.sql
002_SeedUsers.cs  // Note: Class must be named _002_SeedUsers or similar
```

**Important Notes:**
- **C# Class Names**: Cannot start with digits, so `002_SeedUsers.cs` requires class name `_002_SeedUsers` or `M002_SeedUsers`
- **Mixed Types**: SQL files and C# classes are sorted together alphabetically
- **Case Sensitivity**: Sorting is case-sensitive, so `M001` comes before `m001`
- **Namespace Impact**: Full namespace is used for sorting, not just file name

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

    public string GetUpgradeScript(CodeScriptContext context)
    {
        // Dynamic SQL generation with database queries
        var userCount = context.ConnectionManager.ExecuteWithManagedConnection(
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

    public string GetDowngradeScript(CodeScriptContext context)
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
      .UseSqlServerProvisioner(connectionString); // Async-first database provisioner
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

##### **How UseStandardFolderStructure Works**

The `UseStandardFolderStructure` method automatically discovers your migration scripts using these steps:

**1. Namespace Detection:**
```csharp
// Analyzes embedded resources to find base namespace
// For resources like: MyApp.Scripts.upgrades.M001_CreateTable.sql
// Discovers base namespace: MyApp.Scripts
string baseNamespace = AssemblyResourceUtility.DiscoverBaseNamespace(assembly);
```

**2. Folder Structure Expected:**
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

**3. Embedded Resource Naming:**
When you mark SQL scripts as **Embedded Resources**, they become:
```
YourAssembly.Scripts.upgrades.M001_CreateUsersTable.sql
YourAssembly.Scripts.upgrades.M002_SeedUsers.cs
YourAssembly.Scripts.downgrades.M001_CreateUsersTable.sql
```

**4. Discovery Process:**
- **SQL Scripts**: Found via `UseEmbeddedScriptsFromFolder(assembly, baseNamespace, "upgrades")`
- **C# Scripts**: Found via `UseCodeScripts(assembly)`
- **Downgrades**: Found via `UseDowngradesFromFolder(assembly, baseNamespace, "downgrades")`

**5. Sorting and Execution:**
All discovered scripts are sorted alphabetically by their full name and executed in order.

**Troubleshooting Discovery Issues:**
```csharp
// If discovery isn't working, manually specify the namespace:
config.UseEmbeddedScriptsFromFolder(assembly, "YourApp.Scripts", "upgrades")
      .UseDowngradesFromFolder(assembly, "YourApp.Scripts", "downgrades")
      .UseCodeScripts(assembly);
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
// Automatic database creation with async-first provisioner
config.CreateDatabaseIfNotExists(creationTemplate: "CREATE DATABASE {0}");

// Database provisioner uses async methods internally
// DatabaseProvisioner.DatabaseExistsAsync()
// DatabaseProvisioner.CreateDatabaseAsync()
// DatabaseProvisioner.EnsureDatabaseExistsAsync()
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

Code scripts receive variables through a `CodeScriptContext` parameter that provides both connection manager and variable access:

```csharp
public class EnvironmentSpecificMigration : ICodeScript
{
    public bool SupportsDowngrade => true;

    public string GetUpgradeScript(CodeScriptContext context)
    {
        // Access variables with the improved API
        string environment = context.Vars.GetString("Environment", "Development");
        string tenantId = context.Vars.GetRequiredString("TenantId"); // Throws if missing
        string adminEmail = context.Vars.GetString("AdminEmail", "admin@example.com");

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

    public string GetDowngradeScript(CodeScriptContext context)
    {
        string tenantId = context.Vars.GetRequiredString("TenantId");
        return $"DELETE FROM AdminUsers WHERE TenantId = '{tenantId}';";
    }
}
```

#### Variable Accessor API

The `VariableAccessor` class provides a fluent API for accessing variables with automatic type conversion and validation:

```csharp
public class TypedVariableExample : ICodeScript
{
    public bool SupportsDowngrade => true;

    public string GetUpgradeScript(CodeScriptContext context)
    {
        // String variables with defaults
        string environment = context.Vars.GetString("Environment", "Development");
        string adminEmail = context.Vars.GetString("AdminEmail", "admin@example.com");
        
        // Required string variables (throws if missing)
        string tenantId = context.Vars.GetRequiredString("TenantId");
        
        // Integer variables with automatic parsing
        int maxRetries = context.Vars.GetInt("MaxRetries", 3);
        int batchSize = context.Vars.GetRequiredInt("BatchSize"); // Throws if missing or invalid
        
        // Boolean variables
        bool enableDebug = context.Vars.GetBool("EnableDebug", false);
        bool isProduction = context.Vars.GetRequiredBool("IsProduction");
        
        // Check if variable exists
        if (context.Vars.HasVariable("SpecialFeature"))
        {
            // Handle special feature configuration
        }
        
        // Get all variable names for debugging
        var allVariables = string.Join(", ", context.Vars.GetVariableNames());
        
        return $@"
            -- Migration for {environment} environment
            -- Tenant: {tenantId}, Debug: {enableDebug}, Production: {isProduction}
            -- Max retries: {maxRetries}, Batch size: {batchSize}
            -- Available variables: {allVariables}
            
            INSERT INTO Configuration (TenantId, Environment, MaxRetries, BatchSize, EnableDebug) 
            VALUES ('{tenantId}', '{environment}', {maxRetries}, {batchSize}, {(enableDebug ? 1 : 0)});
        ";
    }

    public string GetDowngradeScript(CodeScriptContext context)
    {
        string tenantId = context.Vars.GetRequiredString("TenantId");
        return $"DELETE FROM Configuration WHERE TenantId = '{tenantId}';";
    }
}
```

**Available Methods:**
- `GetString(key, defaultValue)` - Get string variable with optional default
- `GetRequiredString(key)` - Get required string variable (throws if missing)
- `GetInt(key, defaultValue)` - Get integer variable with automatic parsing
- `GetRequiredInt(key)` - Get required integer variable (throws if missing/invalid)
- `GetBool(key, defaultValue)` - Get boolean variable with automatic parsing
- `GetRequiredBool(key)` - Get required boolean variable (throws if missing/invalid)
- `HasVariable(key)` - Check if variable exists
- `GetVariableNames()` - Get all variable names

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

1. **Use VariableAccessor API** for cleaner, type-safe variable access
2. **Use descriptive variable names** that clearly indicate their purpose
3. **Use GetRequired methods** for critical variables to fail fast with clear error messages
4. **Keep variable values simple** - avoid complex JSON or special characters in SQL substitution
5. **Document required variables** for each migration that uses them

```csharp
// Example of robust variable handling with VariableAccessor
public string GetUpgradeScript(CodeScriptContext context)
{
    // Required variables throw clear exceptions if missing
    string tenantId = context.Vars.GetRequiredString("TenantId");
    
    // Optional variables with sensible defaults
    string environment = context.Vars.GetString("Environment", "Development");
    int maxRetries = context.Vars.GetInt("MaxRetries", 3);
    
    // Type-safe boolean handling
    bool enableFeature = context.Vars.GetBool("EnableFeature", false);

    return $@"
        INSERT INTO TenantConfiguration (TenantId, Environment, MaxRetries, EnableFeature) 
        VALUES ('{tenantId}', '{environment}', {maxRetries}, {(enableFeature ? 1 : 0)});
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

    public string GetUpgradeScript(CodeScriptContext context)
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

    public string GetDowngradeScript(CodeScriptContext context)
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

    public string GetUpgradeScript(CodeScriptContext context)
    {
        var sb = new StringBuilder();
        
        // Check if index already exists
        var indexExists = context.ConnectionManager.ExecuteWithManagedConnection(
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
        var recordCount = context.ConnectionManager.ExecuteWithManagedConnection(
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

    public string GetDowngradeScript(CodeScriptContext context)
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

##### **Naming Strategy Guidelines**

**Core Principle:** Use a consistent naming convention that sorts correctly across all file types.

**Recommended Approaches:**

**Option 1: M### Prefix (Recommended)**
```
M001_CreateUsersTable.sql
M002_SeedUsers.cs
M003_CreateProductsTable.sql
M004_CreateOrdersTable.sql
M005_EnvironmentSpecificSettings.sql
```

**Option 2: Date-based (Good for teams)**
```
20250716_001_CreateUsersTable.sql
20250716_002_SeedUsers.cs
20250716_003_CreateProductsTable.sql
```

**Option 3: Semantic Versioning**
```
V1_0_1_CreateUsersTable.sql
V1_0_2_SeedUsers.cs
V1_0_3_CreateProductsTable.sql
```

**Why These Work:**
- ✅ **Alphabetical sorting** works correctly
- ✅ **C# class compatibility** - all are valid C# class names
- ✅ **Database agnostic** - works with any database technology
- ✅ **File type agnostic** - works with .sql, .cs, .json, .xml, .py, .js, etc.
- ✅ **Self-documenting** - clear sequence and purpose

**What to Avoid:**
```
❌ 001_CreateTable.sql + _002_SeedUsers.cs  // Inconsistent prefixes
❌ createTable.sql + SeedUsers.cs          // No ordering
❌ 1_create.sql + 10_seed.cs              // Incorrect alphabetical order
```

##### **Folder Organization**
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

    public string GetUpgradeScript(CodeScriptContext context)
    {
        // ✅ Good: Validate preconditions
        var tableExists = context.ConnectionManager.ExecuteWithManagedConnection(
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

    public string GetDowngradeScript(CodeScriptContext context)
    {
        // ✅ Good: Provide complete rollback
        return @"
            -- Remove preferences column
            ALTER TABLE Users DROP COLUMN Preferences;
        ";
    }
}
```

## Creating Custom Providers and Extensions

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

### Custom Database Provisioner

The `IDatabaseProvisioner` interface uses async-first architecture for database creation and management:

```csharp
using DbReactor.Core.Provisioning;

public class PostgresDatabaseProvisioner : IDatabaseProvisioner
{
    private readonly string _connectionString;
    private readonly ILogProvider _logProvider;

    public PostgresDatabaseProvisioner(string connectionString, ILogProvider logProvider = null)
    {
        _connectionString = connectionString;
        _logProvider = logProvider ?? new NullLogProvider();
    }

    public async Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            string databaseName = builder.Database;
            
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new InvalidOperationException("Connection string must specify a database name");
            }

            // Connect to postgres database to check if target database exists
            builder.Database = "postgres";
            using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            using var cmd = new NpgsqlCommand(
                "SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = @dbName)", 
                connection);
            cmd.Parameters.AddWithValue("@dbName", databaseName);
            
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return (bool)result;
        }
        catch (Exception ex)
        {
            _logProvider?.WriteError($"Error checking if database exists: {ex.Message}");
            throw;
        }
    }

    public async Task CreateDatabaseAsync(string template = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            string databaseName = builder.Database;
            
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new InvalidOperationException("Connection string must specify a database name");
            }

            // Connect to postgres database to create target database
            builder.Database = "postgres";
            using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            string createSql = template ?? $"CREATE DATABASE \"{databaseName}\"";
            if (template != null)
            {
                createSql = string.Format(template, databaseName);
            }

            _logProvider?.WriteInformation($"Creating database: {databaseName}");
            
            using var cmd = new NpgsqlCommand(createSql, connection);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            
            _logProvider?.WriteInformation($"Database created successfully: {databaseName}");
        }
        catch (Exception ex)
        {
            _logProvider?.WriteError($"Error creating database: {ex.Message}");
            throw;
        }
    }

    public async Task EnsureDatabaseExistsAsync(string template = null, CancellationToken cancellationToken = default)
    {
        if (!await DatabaseExistsAsync(cancellationToken))
        {
            await CreateDatabaseAsync(template, cancellationToken);
        }
        else
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            string databaseName = builder.Database;
            _logProvider?.WriteInformation($"Database already exists: {databaseName}");
        }
    }
}

// Usage with PostgreSQL provider
var config = new DbReactorConfiguration()
    .WithPostgreSQL(connectionString)
    .WithLogToConsole()
    .WithEmbeddedScripts(typeof(Program).Assembly);

// Or configure database provisioner separately
config.DatabaseProvisioner = new PostgresDatabaseProvisioner(connectionString, config.LogProvider);
config.CreateDatabaseIfNotExists();
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

#### 4. Async-First Architecture
DbReactor uses async-first architecture. All core operations are async with sync extension methods:

```csharp
public class CustomConnectionManager : IConnectionManager
{
    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task ExecuteWithManagedConnectionAsync(Func<IDbConnection, Task> operation, CancellationToken cancellationToken = default)
    {
        using var connection = await CreateConnectionAsync(cancellationToken);
        await operation(connection);
    }

    public async Task<T> ExecuteWithManagedConnectionAsync<T>(Func<IDbConnection, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        using var connection = await CreateConnectionAsync(cancellationToken);
        return await operation(connection);
    }
}
```

## Testing

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
        var context = new CodeScriptContext(mockConnectionManager.Object);
        
        // Act
        var script = migration.GetUpgradeScript(context);
        
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
        var context = new CodeScriptContext(mockConnectionManager.Object);
        
        // Act
        var script = migration.GetDowngradeScript(context);
        
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
    public async Task CompleteFlow_UpgradeAndDowngrade_Success()
    {
        // Arrange
        var config = new DbReactorConfiguration()
            .UseSqlServer(TestConnectionString)
            .CreateDatabaseIfNotExists()
            .UseEmbeddedScripts(typeof(IntegrationTests).Assembly);

        var engine = new DbReactorEngine(config);

        try
        {
            // Act - Run upgrades (async)
            var upgradeResult = await engine.ApplyUpgradesAsync();
            
            // Assert - Upgrades successful
            Assert.IsTrue(upgradeResult.Successful);
            
            // Act - Run downgrades (async)
            var downgradeResult = await engine.ApplyDowngradesAsync();
            
            // Assert - Downgrades successful
            Assert.IsTrue(downgradeResult.Successful);
        }
        finally
        {
            // Cleanup test database
            CleanupTestDatabase();
        }
    }

    [TestMethod]
    public void CompleteFlow_SyncWrapper_Success()
    {
        // Arrange
        var config = new DbReactorConfiguration()
            .UseSqlServer(TestConnectionString)
            .CreateDatabaseIfNotExists()
            .UseEmbeddedScripts(typeof(IntegrationTests).Assembly);

        var engine = new DbReactorEngine(config);

        try
        {
            // Act - Run upgrades (sync wrapper)
            var upgradeResult = engine.ApplyUpgrades();
            
            // Assert - Upgrades successful
            Assert.IsTrue(upgradeResult.Successful);
            
            // Act - Run downgrades (sync wrapper)
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

## Performance Optimization

### Best Practices for Large Databases

#### 1. Batch Operations
```csharp
public class LargeDataMigration : ICodeScript
{
    public bool SupportsDowngrade => false;

    public string GetUpgradeScript(CodeScriptContext context)
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
    public bool SupportsDowngrade => true;

    public string GetUpgradeScript(CodeScriptContext context)
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

    public string GetDowngradeScript(CodeScriptContext context)
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

## API Reference

### Core Classes

#### DbReactorEngine
Main entry point for migration operations.

**Async Methods:**
- `Task<DbReactorResult> RunAsync(CancellationToken cancellationToken = default)` - Execute full migration process
- `Task<DbReactorResult> ApplyUpgradesAsync(CancellationToken cancellationToken = default)` - Apply pending upgrades only
- `Task<DbReactorResult> ApplyDowngradesAsync(CancellationToken cancellationToken = default)` - Apply downgrades for removed migrations
- `Task<bool> HasPendingUpgradesAsync(CancellationToken cancellationToken = default)` - Check if upgrades are needed
- `Task<IEnumerable<IMigration>> GetPendingUpgradesAsync(CancellationToken cancellationToken = default)` - Get list of pending migrations
- `Task<IEnumerable<IMigration>> GetAppliedUpgradesAsync(CancellationToken cancellationToken = default)` - Get list of applied migrations

**Sync Extension Methods:**
- `DbReactorResult Run()` - Synchronous wrapper for RunAsync
- `DbReactorResult ApplyUpgrades()` - Synchronous wrapper for ApplyUpgradesAsync
- `DbReactorResult ApplyDowngrades()` - Synchronous wrapper for ApplyDowngradesAsync
- `bool HasPendingUpgrades()` - Synchronous wrapper for HasPendingUpgradesAsync

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
- `bool SupportsDowngrade { get; }` - Whether downgrade is available

**Methods:**
- `string GetUpgradeScript(CodeScriptContext context)` - Generate upgrade SQL
- `string GetDowngradeScript(CodeScriptContext context)` - Generate downgrade SQL

#### CodeScriptContext
Context object that provides access to database connections and variables for code script execution.

**Properties:**
- `IConnectionManager ConnectionManager { get; }` - Database connection manager
- `IReadOnlyDictionary<string, string> Variables { get; }` - Raw variables dictionary
- `VariableAccessor Vars { get; }` - Fluent API for variable access with type conversion

#### VariableAccessor
Provides a fluent API for accessing variables with automatic type conversion and validation.

**Methods:**
- `string GetString(string key, string defaultValue = null)` - Get string variable
- `string GetRequiredString(string key)` - Get required string variable
- `int GetInt(string key, int defaultValue = 0)` - Get integer variable
- `int GetRequiredInt(string key)` - Get required integer variable
- `bool GetBool(string key, bool defaultValue = false)` - Get boolean variable
- `bool GetRequiredBool(string key)` - Get required boolean variable
- `bool HasVariable(string key)` - Check if variable exists
- `IEnumerable<string> GetVariableNames()` - Get all variable names

#### IScriptProvider
Interface for discovering migration scripts.

**Methods:**
- `IEnumerable<IScript> GetScripts()` - Return all discovered scripts

#### IMigrationJournal
Interface for tracking migration execution (async-first with CancellationToken support).

**Methods:**
- `Task EnsureTableExistsAsync(IConnectionManager connectionManager, CancellationToken cancellationToken = default)` - Create journal table
- `Task StoreExecutedMigrationAsync(IMigration migration, MigrationResult result, CancellationToken cancellationToken = default)` - Record execution
- `Task RemoveExecutedMigrationAsync(string upgradeScriptHash, CancellationToken cancellationToken = default)` - Remove from history
- `Task<IEnumerable<MigrationJournalEntry>> GetExecutedMigrationsAsync(CancellationToken cancellationToken = default)` - Get execution history
- `Task<bool> HasBeenExecutedAsync(IMigration migration, CancellationToken cancellationToken = default)` - Check if migration was run

## Contributing

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

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 🚀 [Quick Start Guide](QUICKSTART.md) - Get running in 5 minutes
- 📚 [Full Documentation](README.md) - Complete feature reference
- 🐛 [Issue Tracker](https://github.com/your-org/dbreactor/issues)
- 💬 [Discussions](https://github.com/your-org/dbreactor/discussions)
- 📧 [Email Support](mailto:support@dbreactor.com)

## Version History

### v1.0.0
- Initial release
- SQL Server provider
- Embedded and Code script support
- Migration journal tracking
- Comprehensive error handling
- Async-first architecture with CancellationToken support
- All core interfaces async (IConnectionManager, IScriptExecutor, IMigrationJournal)
- Sync extension methods for backward compatibility
- Improved VariableAccessor API with type conversion
- Enhanced SQL Server provider with async operations

---

Built for the .NET community