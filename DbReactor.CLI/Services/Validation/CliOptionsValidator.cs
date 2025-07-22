using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services.Validation;

public class CliOptionsValidator : ICliOptionsValidator
{
    public IEnumerable<ValidationResult> Validate(CliOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            yield return ValidationResult.Error("Connection String", "Connection string is required");
        }
        else
        {
            yield return ValidationResult.Success("Connection String", "Connection string provided");
        }

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            yield return ValidationResult.Error("Provider", "Database provider is required");
        }
        else
        {
            yield return ValidationResult.Success("Provider", $"Using provider: {options.Provider}");
        }
    }
}