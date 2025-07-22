using System.CommandLine;

namespace DbReactor.CLI.Services.Interactive;

public interface ICommandExecutor
{
    Task<int> ExecuteCommandAsync(string commandName, string[] args);
}