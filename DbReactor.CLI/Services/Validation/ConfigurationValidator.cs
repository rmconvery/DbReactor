using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services.Validation;

public class ConfigurationValidator : IConfigurationValidator
{
    private readonly ICliOptionsValidator _optionsValidator;
    private readonly IPathValidator _pathValidator;
    private readonly IConfigurationBuildValidator _buildValidator;

    public ConfigurationValidator(
        ICliOptionsValidator optionsValidator,
        IPathValidator pathValidator,
        IConfigurationBuildValidator buildValidator)
    {
        _optionsValidator = optionsValidator;
        _pathValidator = pathValidator;
        _buildValidator = buildValidator;
    }

    public async Task<IEnumerable<ValidationResult>> ValidateAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        var results = new List<ValidationResult>();

        // Delegate to specialized validators following Single Responsibility Principle
        results.AddRange(_optionsValidator.Validate(options));
        results.AddRange(_pathValidator.ValidatePaths(options));
        results.AddRange(await _buildValidator.ValidateAsync(options, cancellationToken));

        return results;
    }
}