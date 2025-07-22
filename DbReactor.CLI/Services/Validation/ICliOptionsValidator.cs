using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services.Validation;

public interface ICliOptionsValidator
{
    IEnumerable<ValidationResult> Validate(CliOptions options);
}