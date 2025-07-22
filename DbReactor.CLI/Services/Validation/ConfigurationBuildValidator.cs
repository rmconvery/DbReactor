using DbReactor.CLI.Configuration;
using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services.Validation;

public class ConfigurationBuildValidator : IConfigurationBuildValidator
{
    private readonly ICliConfigurationService _configurationService;

    public ConfigurationBuildValidator(ICliConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public async Task<IEnumerable<ValidationResult>> ValidateAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _configurationService.BuildConfigurationAsync(options, cancellationToken);
            
            return ValidateBuiltConfiguration(config);
        }
        catch (Exception ex)
        {
            return new[] { ValidationResult.Error("Configuration Build", $"Failed to build configuration: {ex.Message}") };
        }
    }

    private IEnumerable<ValidationResult> ValidateBuiltConfiguration(Core.Configuration.DbReactorConfiguration config)
    {
        yield return ValidationResult.Success("Configuration Build", "Configuration built successfully");

        // Validate script providers
        if (!config.ScriptProviders.Any())
        {
            yield return ValidationResult.Warning("Script Providers", "No script providers configured");
        }
        else
        {
            yield return ValidationResult.Success("Script Providers", $"{config.ScriptProviders.Count} script provider(s) configured");
        }

        // Check downgrade support
        if (config.DowngradeResolver == null)
        {
            yield return ValidationResult.Info("Downgrade Support", "No downgrade resolver configured - rollback operations not available");
        }
        else
        {
            yield return ValidationResult.Success("Downgrade Support", "Downgrade resolver configured - rollback operations available");
        }
    }
}