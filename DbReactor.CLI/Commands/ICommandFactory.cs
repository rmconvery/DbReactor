using System.CommandLine;

namespace DbReactor.CLI.Commands;

public interface ICommandFactory
{
    Command CreateMigrateCommand();
    Command CreateStatusCommand();
    Command CreateRollbackCommand();
    Command CreateCreateScriptCommand();
    Command CreateValidateCommand();
}