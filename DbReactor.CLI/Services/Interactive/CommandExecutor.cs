using DbReactor.CLI.Commands;
using DbReactor.CLI.Constants;
using Spectre.Console;
using System.CommandLine;

namespace DbReactor.CLI.Services.Interactive;

public class CommandExecutor : ICommandExecutor
{
    private readonly ICommandFactory _commandFactory;

    public CommandExecutor(ICommandFactory commandFactory)
    {
        _commandFactory = commandFactory;
    }

    public async Task<int> ExecuteCommandAsync(string commandName, string[] args)
    {
        try
        {
            AnsiConsole.MarkupLine($"[blue]Executing {commandName} command...[/]");
            AnsiConsole.WriteLine();

            Command command = GetCommand(commandName);
            return await command.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return ExitCodes.GeneralError;
        }
    }

    private System.CommandLine.Command GetCommand(string commandName)
    {
        return commandName switch
        {
            "migrate" => _commandFactory.CreateMigrateCommand(),
            "status" => _commandFactory.CreateStatusCommand(),
            "rollback" => _commandFactory.CreateRollbackCommand(),
            "init" => _commandFactory.CreateInitCommand(),
            "create-script" => _commandFactory.CreateCreateScriptCommand(),
            "validate" => _commandFactory.CreateValidateCommand(),
            _ => throw new ArgumentException($"Unknown command: {commandName}")
        };
    }
}