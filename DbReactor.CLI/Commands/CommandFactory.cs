using System.CommandLine;

namespace DbReactor.CLI.Commands;

public class CommandFactory : ICommandFactory
{
    private readonly MigrateCommand _migrateCommand;
    private readonly StatusCommand _statusCommand;
    private readonly RollbackCommand _rollbackCommand;
    private readonly InitCommand _initCommand;
    private readonly CreateScriptCommand _createScriptCommand;
    private readonly ValidateCommand _validateCommand;

    public CommandFactory(
        MigrateCommand migrateCommand,
        StatusCommand statusCommand,
        RollbackCommand rollbackCommand,
        InitCommand initCommand,
        CreateScriptCommand createScriptCommand,
        ValidateCommand validateCommand)
    {
        _migrateCommand = migrateCommand;
        _statusCommand = statusCommand;
        _rollbackCommand = rollbackCommand;
        _initCommand = initCommand;
        _createScriptCommand = createScriptCommand;
        _validateCommand = validateCommand;
    }

    public Command CreateMigrateCommand() => _migrateCommand.BuildCommand();
    public Command CreateStatusCommand() => _statusCommand.BuildCommand();
    public Command CreateRollbackCommand() => _rollbackCommand.BuildCommand();
    public Command CreateInitCommand() => _initCommand.BuildCommand();
    public Command CreateCreateScriptCommand() => _createScriptCommand.BuildCommand();
    public Command CreateValidateCommand() => _validateCommand.BuildCommand();
}