# DbReactor.Core - Quick Start Guide

Get started with DbReactor.Core database migrations in 5 minutes!

## Important Note

**DbReactor.Core is the foundation library and cannot run migrations by itself.** You need to install a database provider package to actually execute migrations.

## 1. Install Packages

```bash
# Core framework (required)
dotnet add package DbReactor.Core

# Database provider (choose one)
dotnet add package DbReactor.MSSqlServer  # For SQL Server
# More providers coming soon...
```

## 2. Basic Project Structure

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

## 3. Configure Your Project File

Mark SQL scripts as **Embedded Resources** in your `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Scripts\\**\\*.sql" />
</ItemGroup>
```

## 4. Write Your Migration Code

### Basic Setup (Program.cs)
```csharp
using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.MSSqlServer.Extensions; // Database provider

class Program
{
    static async Task Main(string[] args)
    {
        var config = new DbReactorConfiguration()
            // Database provider (from extension package)
            .UseSqlServer("Server=localhost;Database=MyApp;Trusted_Connection=true;")
            
            // Core features
            .UseStandardFolderStructure(typeof(Program).Assembly)
            .UseCodeScripts(typeof(Program).Assembly)
            .UseConsoleLogging()
            .CreateDatabaseIfNotExists();

        var engine = new DbReactorEngine(config);

        // Preview migrations before execution (dry run)
        var dryRunResult = await engine.PreviewRunAsync();
        Console.WriteLine($"Would execute {dryRunResult.PendingMigrations} migrations");

        // Execute migrations
        var result = await engine.RunAsync();

        if (result.Successful)
        {
            Console.WriteLine("Migration completed successfully!");
        }
        else
        {
            Console.WriteLine($"Migration failed: {result.ErrorMessage}");
        }
    }
}
```

## 5. Core Configuration Options

### Script Discovery
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    
    // Standard folder structure (Scripts/upgrades, Scripts/downgrades)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    
    // Include C# code scripts
    .UseCodeScripts(typeof(Program).Assembly)
    
    // Custom folder structure
    .UseEmbeddedScriptsFromFolder(assembly, "MyApp.Migrations", "upgrades");
```

### Execution Order
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseAscendingOrder()   // M001, M002, M003... (default)
    .UseDescendingOrder(); // M003, M002, M001...
```

### Variables
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseVariables(new Dictionary<string, string> {
        {"Environment", "Production"},
        {"AdminEmail", "admin@company.com"}
    });
```

### Logging
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseConsoleLogging()              // Built-in console logging
    .UseFileLogging("logs/db.log")    // Built-in file logging
    .LogProvider = new MyLogProvider(); // Custom logging
```

## 6. Running Migrations

### Dry Run (Preview)
```csharp
var engine = new DbReactorEngine(config);

// Preview what would be executed
var dryRunResult = await engine.PreviewRunAsync();

Console.WriteLine($"Total migrations: {dryRunResult.TotalMigrations}");
Console.WriteLine($"Pending upgrades: {dryRunResult.PendingUpgrades}");
Console.WriteLine($"Pending downgrades: {dryRunResult.PendingDowngrades}");
Console.WriteLine($"Already executed: {dryRunResult.SkippedMigrations}");

// Detailed logging is automatically sent to your configured log provider
```

### Basic Execution
```csharp
var engine = new DbReactorEngine(config);
var result = await engine.RunAsync();

if (result.Successful)
{
    Console.WriteLine("Migrations completed!");
}
```

### Get Pending Migrations
```csharp
var pendingMigrations = await engine.GetPendingUpgradesAsync();
Console.WriteLine($"Found {pendingMigrations.Count()} pending migrations");
```

### Downgrade Support
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .AllowDowngrades(true);

var engine = new DbReactorEngine(config);
var result = await engine.RunDowngradeAsync();
```

## 7. Example Migration Files

### SQL Migration (M001_CreateUsersTable.sql)
```sql
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

### C# Migration (M002_SeedUsers.cs)
```csharp
using DbReactor.Core.Abstractions;
using DbReactor.Core.Models.Contexts;

public class M002_SeedUsers : ICodeScript
{
    public bool SupportsDowngrade => true;

    public string GetUpgradeScript(CodeScriptContext context)
    {
        var env = context.Vars.GetString("Environment", "Development");
        return $@"
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

## 8. Advanced Features

### Multiple Script Sources
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseEmbeddedScriptsFromFolder(coreAssembly, "Core.Scripts", "upgrades")
    .UseCodeScripts(migrationAssembly, "Migrations.Code")
    .UseEmbeddedScriptsFromFolder(moduleAssembly, "Module.Scripts", "migrations");
```

### Error Handling
```csharp
try
{
    var result = await engine.RunAsync();
    
    if (result.Successful)
    {
        Console.WriteLine($"Executed {result.ExecutedMigrations.Count()} migrations");
    }
    else
    {
        Console.WriteLine($"Failed: {result.ErrorMessage}");
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

## 9. Database Provider Packages

DbReactor.Core works with database provider packages:

- **[DbReactor.MSSqlServer](../DbReactor.MSSqlServer/README.md)** - Full SQL Server support
- **DbReactor.PostgreSQL** - PostgreSQL support (coming soon)
- **DbReactor.MySQL** - MySQL support (coming soon)
- **DbReactor.SQLite** - SQLite support (coming soon)

## 10. Next Steps

- 📚 **Provider Documentation**: See your database provider's README for specific features
- 🔧 **Advanced Configuration**: Explore custom script discovery and logging
- 🧪 **Testing**: Add unit tests for your migration scripts
- 🚀 **Production**: Set up proper logging and error handling

## Troubleshooting

**Scripts not found?**
```csharp
// Debug script discovery
var pendingMigrations = await engine.GetPendingUpgradesAsync();
Console.WriteLine($"Found {pendingMigrations.Count()} migrations");

// Or specify exact namespace
config.UseEmbeddedScriptsFromFolder(assembly, "YourApp.Scripts", "upgrades");
```

**Need database-specific help?** Check your database provider's documentation for connection strings, timeouts, and other provider-specific features.

---

**That's it!** You now have a working database migration system with DbReactor.Core.

For database-specific features like timeouts, connection pooling, and advanced configuration, see your database provider's documentation.