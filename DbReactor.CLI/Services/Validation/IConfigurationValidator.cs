using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services.Validation;

public interface IConfigurationValidator
{
    Task<IEnumerable<ValidationResult>> ValidateAsync(CliOptions options, CancellationToken cancellationToken = default);
}