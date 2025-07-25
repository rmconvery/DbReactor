using DbReactor.CLI.Models;
using DbReactor.CLI.Services;

namespace DbReactor.CLI.Services.Validation;

public class PathValidator : IPathValidator
{
    private readonly IDirectoryService _directoryService;

    public PathValidator(IDirectoryService directoryService)
    {
        _directoryService = directoryService;
    }

    public IEnumerable<ValidationResult> ValidatePaths(CliOptions options)
    {
        var (upgradesPath, downgradesPath) = _directoryService.DetermineScriptPaths(
            options.UpgradesPath, 
            options.DowngradesPath, 
            options.EnsureDirectories);

        return ValidateUpgradesPath(upgradesPath)
            .Concat(ValidateDowngradesPath(downgradesPath));
    }

    private IEnumerable<ValidationResult> ValidateUpgradesPath(string? upgradesPath)
    {
        if (string.IsNullOrWhiteSpace(upgradesPath))
        {
            yield return ValidationResult.Warning("Upgrades Path", "No upgrades path specified, will use current directory");
        }
        else if (!Directory.Exists(upgradesPath))
        {
            yield return ValidationResult.Error("Upgrades Path", $"Upgrades directory does not exist: {upgradesPath}");
        }
        else
        {
            var sqlFiles = Directory.GetFiles(upgradesPath, "*.sql").Length;
            yield return ValidationResult.Success("Upgrades Path", $"Upgrades directory found: {upgradesPath} ({sqlFiles} SQL files)");
        }
    }

    private IEnumerable<ValidationResult> ValidateDowngradesPath(string? downgradesPath)
    {
        if (string.IsNullOrWhiteSpace(downgradesPath))
        {
            yield return ValidationResult.Warning("Downgrades Path", "No downgrades path specified, rollback operations may not be available");
        }
        else if (!Directory.Exists(downgradesPath))
        {
            yield return ValidationResult.Error("Downgrades Path", $"Downgrades directory does not exist: {downgradesPath}");
        }
        else
        {
            var sqlFiles = Directory.GetFiles(downgradesPath, "*.sql").Length;
            yield return ValidationResult.Success("Downgrades Path", $"Downgrades directory found: {downgradesPath} ({sqlFiles} SQL files)");
        }
    }
}