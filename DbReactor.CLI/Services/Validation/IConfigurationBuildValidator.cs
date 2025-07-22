using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services.Validation;

public interface IConfigurationBuildValidator
{
    Task<IEnumerable<ValidationResult>> ValidateAsync(CliOptions options, CancellationToken cancellationToken = default);
}