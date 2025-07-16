# DbReactor - .NET Database Migration Framework

DbReactor is a powerful, extensible .NET database migration framework that provides version-controlled, repeatable database schema management. It supports multiple script types, database providers, and offers comprehensive tracking and rollback capabilities.

## Packages

DbReactor is split into separate NuGet packages:

### Core Framework
- **[DbReactor.Core](DbReactor.Core/)** - Core migration framework and abstractions
  - [Documentation](DbReactor.Core/README.md) | [Quick Start](DbReactor.Core/QUICKSTART.md)
  - `dotnet add package DbReactor.Core`

### Database Providers
- **[DbReactor.MSSqlServer](DbReactor.MSSqlServer/)** - SQL Server implementation
  - [Documentation](DbReactor.MSSqlServer/README.md) | [Quick Start](DbReactor.MSSqlServer/QUICKSTART.md)
  - `dotnet add package DbReactor.MSSqlServer`

*More database providers coming soon...*

## Table of Contents

- [Quick Start](#quick-start) - Get running in 5 minutes
- [Features](#features) - Key capabilities
- [Architecture](#architecture) - How it works
- [Package Documentation](#package-documentation) - Detailed guides
- [Examples](#examples) - Sample projects
- [Support](#support) - Help and resources

## Features

- **Multiple Script Types**: SQL scripts, dynamic C# code scripts, and embedded resources
- **Database Agnostic**: Extensible architecture supports any database platform
- **Comprehensive Tracking**: Full migration history with execution time and rollback support
- **Dry Run Mode**: Preview what migrations would be executed without actually running them
- **Safe Migrations**: Built-in SQL injection protection and transaction management
- **Flexible Discovery**: Multiple ways to organize and discover migration scripts
- **Async-First Architecture**: Non-blocking database operations with cancellation tokens and sync wrappers (including DatabaseProvisioner)
- **Robust Error Handling**: Detailed exception hierarchy with contextual information
- **Production Ready**: Enterprise-grade security and performance optimizations

## Quick Start

**Choose your database provider to get started:**

- **SQL Server**: [DbReactor.MSSqlServer Quick Start](DbReactor.MSSqlServer/QUICKSTART.md)
- **Core Framework**: [DbReactor.Core Quick Start](DbReactor.Core/QUICKSTART.md)

### 5-Minute Setup

```bash
# Install packages
dotnet add package DbReactor.Core
dotnet add package DbReactor.MSSqlServer
```

```csharp
using DbReactor.MSSqlServer.Extensions;

var config = new DbReactorConfiguration()
    .UseSqlServer(connectionString)
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseConsoleLogging()
    .CreateDatabaseIfNotExists();

var engine = new DbReactorEngine(config);
var result = await engine.RunAsync();
```

**That's it!** See the [quick start guides](#quick-start) for detailed setup instructions.

## Architecture

DbReactor uses a modular architecture with clear separation of concerns:

### Core Framework (`DbReactor.Core`)
- **Migration Engine**: Orchestrates migration execution
- **Configuration Management**: Centralized configuration system
- **Script Discovery**: Finds and orders migration scripts
- **Abstractions**: Database-agnostic interfaces

### Database Providers
- **Connection Management**: Database-specific connection handling
- **Script Execution**: Database-specific SQL execution
- **Migration Journaling**: Tracks executed migrations
- **Database Provisioning**: Creates databases if needed

### Key Interfaces
```csharp
// Database provider interfaces
IConnectionManager     // Database connections
IScriptExecutor       // Script execution
IMigrationJournal     // Migration tracking
IDatabaseProvisioner  // Database creation

// Core interfaces
IScriptProvider       // Script discovery
ILogProvider         // Logging
ICodeScript          // C# migration scripts
```

## Package Documentation

### Core Framework
- **[DbReactor.Core](DbReactor.Core/README.md)** - Core abstractions and engine
- **[DbReactor.Core Quick Start](DbReactor.Core/QUICKSTART.md)** - Get started in 5 minutes

### Database Providers
- **[DbReactor.MSSqlServer](DbReactor.MSSqlServer/README.md)** - Complete SQL Server implementation
- **[DbReactor.MSSqlServer Quick Start](DbReactor.MSSqlServer/QUICKSTART.md)** - SQL Server setup guide

## Examples

### Basic SQL Server Migration
```csharp
// Program.cs
var config = new DbReactorConfiguration()
    .UseSqlServer("Server=localhost;Database=MyApp;Trusted_Connection=true;")
    .UseStandardFolderStructure(typeof(Program).Assembly)
    .UseConsoleLogging();

var engine = new DbReactorEngine(config);
await engine.RunAsync();
```

### Migration Files
```
Scripts/
├── upgrades/
│   ├── M001_CreateUsersTable.sql
│   ├── M002_SeedUsers.cs
│   └── M003_CreateIndexes.sql
└── downgrades/
    ├── M001_CreateUsersTable.sql
    ├── M002_SeedUsers.sql
    └── M003_CreateIndexes.sql
```

### Sample Projects
- **[DbReactor.RunTest](DbReactor.RunTest/)** - Complete working example
- More examples in package documentation

## Creating Database Providers

DbReactor is designed to be extensible. Create custom database providers by implementing the core interfaces:

```csharp
public static class MyDatabaseExtensions
{
    public static DbReactorConfiguration UseMyDatabase(this DbReactorConfiguration config, string connectionString)
    {
        return config
            .UseConnectionManager(new MyConnectionManager(connectionString))
            .UseScriptExecutor(new MyScriptExecutor())
            .UseMigrationJournal(new MyMigrationJournal())
            .UseDatabaseProvisioner(new MyDatabaseProvisioner());
    }
}
```

## Roadmap

### Upcoming Database Providers
- **DbReactor.PostgreSQL** - PostgreSQL support
- **DbReactor.MySQL** - MySQL support  
- **DbReactor.SQLite** - SQLite support
- **DbReactor.Oracle** - Oracle support

### Planned Features
- Migration rollback improvements
- Schema comparison tools
- Migration performance analytics
- Multi-database support
- Cloud database provider integrations

## Support

- **Package Documentation**: See individual package READMEs for detailed guides
- **Quick Start Guides**: Step-by-step setup instructions
- **Issues**: [GitHub Issues](https://github.com/your-org/DbReactor/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-org/DbReactor/discussions)

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

[Your License Here]

---

**Ready to get started?** Choose your database provider and follow the quick start guide!
