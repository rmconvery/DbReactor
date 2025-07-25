namespace DbReactor.CLI.Services;

public interface IDirectoryService
{
    (string? UpgradesPath, string? DowngradesPath) DetermineScriptPaths(string? upgradesPath, string? downgradesPath, bool ensureDirectories);
    void EnsureDirectoryExists(string path);
    void ValidateDirectoryExists(string path, string pathType);
}