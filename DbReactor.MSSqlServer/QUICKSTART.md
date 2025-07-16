# DbReactor.MSSqlServer - Quick Start Guide

Get your SQL Server database migrations running in 5 minutes!

## 1. Install Packages

```bash
# Core framework + SQL Server provider
dotnet add package DbReactor.Core
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
  <EmbeddedResource Include="Scripts\\**\\*.sql" />
</ItemGroup>
```

## 4. Write Your Migration Code

### Basic Setup (Program.cs)
```csharp
using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.MSSqlServer.Extensions;

class Program
{
    static async Task Main(string[] args)
    {
        string connectionString = "Server=localhost;Database=MyApp;Trusted_Connection=true;TrustServerCertificate=true;";

        var config = new DbReactorConfiguration()
            .UseSqlServer(connectionString)
            .UseStandardFolderStructure(typeof(Program).Assembly)
            .UseCodeScripts(typeof(Program).Assembly)
            .UseConsoleLogging()
            .CreateDatabaseIfNotExists();

        var engine = new DbReactorEngine(config);
        
        // Preview migrations before execution (dry run)
        var dryRunResult = await engine.PreviewRunAsync();
        Console.WriteLine($"Would execute {dryRunResult.PendingMigrations} migrations");

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

## 5. SQL Server Configuration Options

### Complete Configuration
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(
        connectionString: "Server=localhost;Database=MyApp;Trusted_Connection=true;",
        commandTimeout: TimeSpan.FromMinutes(5),
        journalSchema: "dbo", 
        journalTable: "__migration_journal"
    )
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseCodeScripts(typeof(Program).Assembly)
    .UseConsoleLogging()
    .CreateDatabaseIfNotExists();
```

### Timeout Configuration
```csharp
// Set timeout during configuration
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString, commandTimeout: TimeSpan.FromSeconds(120))
    .UseStandardFolderStructure(typeof(Program).Assembly);

// Or configure timeout separately
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseSqlServerCommandTimeout(TimeSpan.FromMinutes(5))
    .UseStandardFolderStructure(typeof(Program).Assembly);
```

### Custom Migration Journal
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString, 
        journalSchema: "migrations",
        journalTable: "MigrationHistory")
    .UseStandardFolderStructure(typeof(Program).Assembly);

// Or configure journal separately
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseSqlServerJournal("custom_schema", "custom_table")
    .UseStandardFolderStructure(typeof(Program).Assembly);
```

## 6. Connection String Examples

### Windows Authentication
```csharp
string connectionString = "Server=localhost;Database=MyApp;Trusted_Connection=true;TrustServerCertificate=true;";
```

### SQL Server Authentication
```csharp
string connectionString = "Server=localhost;Database=MyApp;User Id=sa;Password=myPassword;TrustServerCertificate=true;";
```

### Azure SQL Database
```csharp
string connectionString = "Server=myserver.database.windows.net;Database=MyApp;User Id=myuser;Password=mypassword;";
```

## 7. Environment-Specific Configuration

### Using Variables
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseVariables(new Dictionary<string, string> {
        {"Environment", "Production"},
        {"AdminEmail", "admin@company.com"},
        {"TenantId", "tenant-123"}
    });
```

### Use Variables in SQL
```sql
-- In your SQL files
INSERT INTO Configuration (Environment, AdminEmail, TenantId) 
VALUES ('${Environment}', '${AdminEmail}', '${TenantId}');
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

## 8. Run Your Migrations

```bash
dotnet run
```

**Expected Output:**
```
[INFO] Starting database migration...
[INFO] Successfully executed script: M001_CreateUsersTable
[INFO] Successfully executed script: M002_SeedUsers
[INFO] Successfully executed script: M003_CreateIndexes
[INFO] Database migration completed. Success: True
Migration completed successfully!
```

## 9. Advanced Features

### Individual Component Configuration
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServerConnection(connectionString)
    .UseSqlServerExecutor(TimeSpan.FromSeconds(60))
    .UseSqlServerJournal("dbo", "__migration_journal")
    .UseSqlServerProvisioner(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly);
```

### Custom Database Creation
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

### Multiple Script Sources
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseEmbeddedScriptsFromFolder(coreAssembly, "Core.Scripts", "upgrades")
    .UseCodeScripts(migrationAssembly, "Migrations.Code")
    .UseEmbeddedScriptsFromFolder(moduleAssembly, "Module.Scripts", "migrations");
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

## 10. Migration Naming Best Practices

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

## 11. Error Handling

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

## 12. Troubleshooting

### Scripts Not Found
```csharp
// Debug script discovery
var pendingMigrations = await engine.GetPendingUpgradesAsync();
Console.WriteLine($"Found {pendingMigrations.Count()} pending migrations");

// Or specify exact namespace
config.UseEmbeddedScriptsFromFolder(assembly, "YourApp.Scripts", "upgrades");
```

### Connection Issues
- Verify SQL Server is running
- Check connection string format
- Ensure user has database permissions
- Test connection with SQL Server Management Studio

### Timeout Issues
- Increase timeout for long-running migrations:
  ```csharp
  .UseSqlServer(connectionString, commandTimeout: TimeSpan.FromMinutes(10))
  ```

## 13. Next Steps

- 📚 **Full Documentation**: See [README.md](README.md) for comprehensive features
- 🔧 **Core Features**: Explore [DbReactor.Core](../DbReactor.Core/README.md) for framework features
- 🧪 **Testing**: Add unit tests for your migration scripts
- 🚀 **Production**: Set up proper logging, error handling, and monitoring

## Common Patterns

### Development vs Production
```csharp
string connectionString = GetConnectionString(); // From config
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseVariables(new Dictionary<string, string> {
        {"Environment", env},
        {"AdminEmail", GetAdminEmail(env)}
    });
```

### Long-Running Migrations
```csharp
var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString, commandTimeout: TimeSpan.FromMinutes(30))
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseConsoleLogging();
```

---

**That's it!** You now have SQL Server database migrations working with DbReactor!

For more advanced features and configuration options, see the [full documentation](README.md).