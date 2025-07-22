using DbReactor.CLI.Models;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Services;

public class ProjectInitializationService : IProjectInitializationService
{
    private readonly ILogger<ProjectInitializationService> _logger;

    public ProjectInitializationService(ILogger<ProjectInitializationService> logger)
    {
        _logger = logger;
    }

    public async Task<CommandResult> InitializeProjectAsync(string targetPath, CancellationToken cancellationToken = default)
    {
        try
        {
            await CreateDirectoryStructure(targetPath);

            _logger.LogInformation("Project initialized successfully at: {TargetPath}", targetPath);
            return CommandResult.Ok($"Project initialized successfully at: {targetPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize project at: {TargetPath}", targetPath);
            return CommandResult.Error($"Failed to initialize project: {ex.Message}", ex);
        }
    }

    private async Task CreateDirectoryStructure(string targetPath)
    {
        var scriptsPath = Path.Combine(targetPath, "Scripts");
        var upgradesPath = Path.Combine(scriptsPath, "upgrades");
        var downgradesPath = Path.Combine(scriptsPath, "downgrades");

        CreateDirectoryIfNotExists(scriptsPath);
        CreateDirectoryIfNotExists(upgradesPath);
        CreateDirectoryIfNotExists(downgradesPath);

        await CreateReadmeFiles(scriptsPath, upgradesPath, downgradesPath);
    }

    private void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            _logger.LogDebug("Created directory: {Path}", path);
        }
        else
        {
            _logger.LogDebug("Directory already exists: {Path}", path);
        }
    }

    private async Task CreateReadmeFiles(string scriptsPath, string upgradesPath, string downgradesPath)
    {
        var scriptsReadme = Path.Combine(scriptsPath, "README.md");
        var upgradesReadme = Path.Combine(upgradesPath, "README.md");
        var downgradesReadme = Path.Combine(downgradesPath, "README.md");

        await CreateReadmeIfNotExists(scriptsReadme, GetScriptsReadmeContent());
        await CreateReadmeIfNotExists(upgradesReadme, GetUpgradesReadmeContent());
        await CreateReadmeIfNotExists(downgradesReadme, GetDowngradesReadmeContent());
    }

    private async Task CreateReadmeIfNotExists(string path, string content)
    {
        if (!File.Exists(path))
        {
            await File.WriteAllTextAsync(path, content);
            _logger.LogDebug("Created README: {Path}", path);
        }
    }


    private static string GetScriptsReadmeContent() => @"# DbReactor Scripts

This directory contains your database migration scripts.

## Structure
- `upgrades/` - Contains migration scripts that move the database forward
- `downgrades/` - Contains migration scripts that rollback changes

## Naming Convention
Use descriptive names with version prefixes:
- `M001_CreateUsersTable.sql`
- `M002_AddIndexes.sql`
- `M003_SeedReferenceData.cs`

## Script Types
- `.sql` files - Standard SQL scripts
- `.cs` files - C# code scripts for complex migrations
";

    private static string GetUpgradesReadmeContent() => @"# Upgrade Scripts

Place your forward migration scripts here.

## Examples
- `M001_CreateUsersTable.sql` - Create the Users table
- `M002_AddUserIndex.sql` - Add index on Users table
- `M003_SeedDefaultUsers.cs` - Seed default user data
";

    private static string GetDowngradesReadmeContent() => @"# Downgrade Scripts

Place your rollback migration scripts here.

Each downgrade script should reverse the changes made by the corresponding upgrade script.

## Examples
- `M001_CreateUsersTable.sql` - Drop the Users table
- `M002_AddUserIndex.sql` - Drop index from Users table
- `M003_SeedDefaultUsers.sql` - Remove default user data
";
}