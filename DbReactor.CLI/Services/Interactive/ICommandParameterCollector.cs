using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services.Interactive;

public interface ICommandParameterCollector
{
    Task<string[]> CollectParametersAsync(string commandName, CliOptions baseConfiguration);
}