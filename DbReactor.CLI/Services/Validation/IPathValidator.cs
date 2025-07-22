using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services.Validation;

public interface IPathValidator
{
    IEnumerable<ValidationResult> ValidatePaths(CliOptions options);
}