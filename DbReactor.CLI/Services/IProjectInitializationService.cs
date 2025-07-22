using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services;

public interface IProjectInitializationService
{
    Task<CommandResult> InitializeProjectAsync(string targetPath, CancellationToken cancellationToken = default);
}