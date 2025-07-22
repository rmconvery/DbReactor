using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services.Interactive;

public interface IInteractiveConfigurationCollector
{
    Task<CliOptions> CollectBaseConfigurationAsync();
}