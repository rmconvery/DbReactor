# DbReactor.Core - Quick Start Guide

Get your database migrations running in 5 minutes!

## 1. Install Packages

```bash
# Core framework
dotnet add package DbReactor.Core

# Database provider (SQL Server example)
dotnet add package DbReactor.MSSqlServer
```

## 2. Create Your Migration Files

### Folder Structure
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

### SQL Migration (M001_CreateUsersTable.sql)
```sql
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
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

## 3. Configure Your Project File

Mark SQL scripts as **Embedded Resources** in your `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Scripts\**\*.sql" />
</ItemGroup>
```

## 4. Write Your Migration Code

### Basic Setup (Program.cs)
```csharp
using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.Core.Extensions;
using DbReactor.MSSqlServer.Extensions;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "Server=localhost;Database=MyApp;Trusted_Connection=true;";

        var config = new DbReactorConfiguration()
            .UseSqlServer(connectionString)
            .UseConsoleLogging()
            .CreateDatabaseIfNotExists()
            .UseStandardFolderStructure(typeof(Program).Assembly);

        var engine = new DbReactorEngine(config);
        var result = engine.Run();

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

### Async Version
```csharp
static async Task Main(string[] args)
{
    // ... same config ...
    
    var result = await engine.RunAsync();
    
    if (result.Successful)
    {
        Console.WriteLine("Migration completed successfully!");
    }
}
```

## 5. Run Your Migrations

```bash
dotnet run
```

**Output:**
```
[INFO] Starting database migration...
[INFO] Successfully executed script: M001_CreateUsersTable
[INFO] Successfully executed script: M002_SeedUsers
[INFO] Successfully executed script: M003_CreateIndexes
[INFO] Database migration completed. Success: True
Migration completed successfully!
```

## 6. Common Patterns

### Environment-Specific Configuration
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(GetConnectionString())
    .UseConsoleLogging()
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseVariables(new Dictionary<string, string> {
        {"Environment", "Production"},
        {"AdminEmail", "admin@company.com"}
    });
```

### Custom Migration Journal Table
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString, 
        journalSchema: "migrations",      // Custom schema
        journalTable: "MigrationHistory") // Custom table name (default: __migration_journal)
    .UseConsoleLogging()
    .UseStandardFolderStructure(typeof(Program).Assembly);

// Or configure individual components
var config = new DbReactorConfiguration()
    .UseSqlServerConnection(connectionString)
    .UseSqlServerExecutor()
    .UseSqlServerJournal("custom_schema", "custom_migration_table")
    .UseStandardFolderStructure(typeof(Program).Assembly);
```

### Use Variables in SQL
```sql
-- In your SQL files
INSERT INTO Configuration (Environment, AdminEmail) 
VALUES ('${Environment}', '${AdminEmail}');
```

### Use Variables in C# Scripts
```csharp
public string GetUpgradeScript(CodeScriptContext context)
{
    string env = context.Vars.GetString("Environment", "Development");
    string adminEmail = context.Vars.GetRequiredString("AdminEmail");
    
    return $"INSERT INTO Users (Username, Email) VALUES ('admin', '{adminEmail}')";
}
```

## 7. Database Providers

### SQL Server
```csharp
.UseSqlServer(connectionString)
```

### Other Databases
Create custom providers by implementing:
- `IConnectionManager`
- `IScriptExecutor` 
- `IMigrationJournal`

## 8. Migration Naming Best Practices

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
001_CreateTable.sql + _002_SeedUsers.cs  // Inconsistent
createTable.sql + SeedUsers.cs          // No ordering
```

## 9. Next Steps

- 📚 **Full Documentation**: See [README.md](README.md) for comprehensive features
- 🔧 **Custom Providers**: Create providers for PostgreSQL, MySQL, MongoDB
- 🚀 **Advanced Features**: Variable substitution, custom discovery, rollbacks
- 🧪 **Testing**: Add unit tests for your migration scripts

## Troubleshooting

**Scripts not found?**
```csharp
// Debug what's being discovered
var pendingMigrations = await engine.GetPendingUpgradesAsync();
Console.WriteLine($"Found {pendingMigrations.Count()} migrations");

// Or manually specify namespace
config.UseEmbeddedScriptsFromFolder(assembly, "YourApp.Scripts", "upgrades");
```

**Need help?** Check the [full documentation](README.md) or create an issue!

---

**That's it!** You now have a working database migration system.