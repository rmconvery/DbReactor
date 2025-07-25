using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Services;

public class DirectoryService : IDirectoryService
{
    private const int PreferredPatternIndex = 0;
    private readonly ILogger<DirectoryService> _logger;

    public DirectoryService(ILogger<DirectoryService> logger)
    {
        _logger = logger;
    }

    public (string? UpgradesPath, string? DowngradesPath) DetermineScriptPaths(
        string? upgradesPath, 
        string? downgradesPath, 
        bool ensureDirectories)
    {
        var resolvedPaths = ResolveDefaultPathsIfNeeded(upgradesPath, downgradesPath, ensureDirectories);
        
        if (ensureDirectories)
        {
            EnsureDirectoriesExist(resolvedPaths.UpgradesPath, resolvedPaths.DowngradesPath);
        }
        else
        {
            ValidateDirectoriesExist(resolvedPaths.UpgradesPath, resolvedPaths.DowngradesPath);
        }

        return resolvedPaths;
    }

    public void EnsureDirectoryExists(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            _logger.LogDebug("Created directory: {Path}", path);
        }
    }

    public void ValidateDirectoryExists(string path, string pathType)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"{pathType} directory not found: {path}. Use --ensure-dirs to create it automatically.");
        }
    }

    private static (string? UpgradesPath, string? DowngradesPath) ResolveDefaultPathsIfNeeded(
        string? upgradesPath, 
        string? downgradesPath, 
        bool ensureDirectories)
    {
        // If both paths are specified, use them as-is
        if (!string.IsNullOrEmpty(upgradesPath) && !string.IsNullOrEmpty(downgradesPath))
        {
            return (upgradesPath, downgradesPath);
        }

        var currentDir = Directory.GetCurrentDirectory();
        
        // Try multiple default patterns in order of preference
        var defaultPatterns = new[]
        {
            (Path.Combine(currentDir, "upgrades"), Path.Combine(currentDir, "downgrades")),
            (Path.Combine(currentDir, "Scripts", "upgrades"), Path.Combine(currentDir, "Scripts", "downgrades"))
        };
        
        // Find existing pattern or use preferred pattern if ensuring directories
        var (defaultUpgrades, defaultDowngrades) = FindExistingPatternOrDefault(defaultPatterns, ensureDirectories);

        // Use specified paths or determined defaults
        var resolvedUpgrades = DetermineUpgradesPath(upgradesPath, defaultUpgrades, ensureDirectories);
        var resolvedDowngrades = DetermineDowngradesPath(downgradesPath, defaultDowngrades, ensureDirectories);

        return (resolvedUpgrades, resolvedDowngrades);
    }

    private static (string defaultUpgrades, string defaultDowngrades) FindExistingPatternOrDefault(
        (string upgrades, string downgrades)[] patterns, 
        bool ensureDirectories)
    {
        // If we're ensuring directories, use the preferred pattern
        if (ensureDirectories)
        {
            return patterns[PreferredPatternIndex];
        }
        
        // Otherwise, find an existing pattern
        foreach (var (upgrades, downgrades) in patterns)
        {
            if (Directory.Exists(upgrades) && Directory.Exists(downgrades))
            {
                return (upgrades, downgrades);
            }
        }
        
        // No existing pattern found, return empty paths to indicate no defaults
        return (string.Empty, string.Empty);
    }
    
    private static string? DetermineUpgradesPath(string? specifiedPath, string defaultPath, bool ensureDirectories)
    {
        if (!string.IsNullOrEmpty(specifiedPath))
        {
            return specifiedPath;
        }
        
        if (string.IsNullOrEmpty(defaultPath))
        {
            return null;
        }

        return ensureDirectories || Directory.Exists(defaultPath) ? defaultPath : null;
    }

    private static string? DetermineDowngradesPath(string? specifiedPath, string defaultPath, bool ensureDirectories)
    {
        if (!string.IsNullOrEmpty(specifiedPath))
        {
            return specifiedPath;
        }
        
        if (string.IsNullOrEmpty(defaultPath))
        {
            return null;
        }

        return ensureDirectories || Directory.Exists(defaultPath) ? defaultPath : null;
    }

    private void EnsureDirectoriesExist(string? upgradesPath, string? downgradesPath)
    {
        if (!string.IsNullOrEmpty(upgradesPath))
        {
            EnsureDirectoryExists(upgradesPath);
        }

        if (!string.IsNullOrEmpty(downgradesPath))
        {
            EnsureDirectoryExists(downgradesPath);
        }
    }

    private void ValidateDirectoriesExist(string? upgradesPath, string? downgradesPath)
    {
        if (!string.IsNullOrEmpty(upgradesPath))
        {
            ValidateDirectoryExists(upgradesPath, "Upgrades");
        }

        if (!string.IsNullOrEmpty(downgradesPath))
        {
            ValidateDirectoryExists(downgradesPath, "Downgrades");
        }
    }
}