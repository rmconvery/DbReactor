using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Services;

public class DirectoryService : IDirectoryService
{
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
        var defaultUpgrades = Path.Combine(currentDir, "Scripts", "upgrades");
        var defaultDowngrades = Path.Combine(currentDir, "Scripts", "downgrades");

        // Use specified paths or determine defaults
        var resolvedUpgrades = DetermineUpgradesPath(upgradesPath, defaultUpgrades, ensureDirectories);
        var resolvedDowngrades = DetermineDowngradesPath(downgradesPath, defaultDowngrades, ensureDirectories);

        return (resolvedUpgrades, resolvedDowngrades);
    }

    private static string? DetermineUpgradesPath(string? specifiedPath, string defaultPath, bool ensureDirectories)
    {
        if (!string.IsNullOrEmpty(specifiedPath))
        {
            return specifiedPath;
        }

        return ensureDirectories || Directory.Exists(defaultPath) ? defaultPath : null;
    }

    private static string? DetermineDowngradesPath(string? specifiedPath, string defaultPath, bool ensureDirectories)
    {
        if (!string.IsNullOrEmpty(specifiedPath))
        {
            return specifiedPath;
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