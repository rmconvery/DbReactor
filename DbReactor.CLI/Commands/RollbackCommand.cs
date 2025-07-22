using System.CommandLine;
using DbReactor.CLI.Configuration;
using DbReactor.CLI.Models;
using DbReactor.CLI.Services;
using DbReactor.Core.Models;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Commands;

public class RollbackCommand : BaseCommand
{
    private readonly IMigrationService _migrationService;
    private readonly IUserInteractionService _userInteractionService;

    public RollbackCommand(
        ICliConfigurationService configurationService,
        IOutputService outputService,
        IMigrationService migrationService,
        IUserInteractionService userInteractionService,
        ILogger<RollbackCommand> logger) 
        : base(configurationService, outputService, logger)
    {
        _migrationService = migrationService;
        _userInteractionService = userInteractionService;
    }

    public override Command BuildCommand()
    {
        var command = new Command("rollback", "Rollback database migrations");

        var connectionStringOption = CreateConnectionStringOption();
        var configOption = CreateConfigOption();
        var upgradesPathOption = CreateUpgradesPathOption();
        var downgradesPathOption = CreateDowngradesPathOption();
        var verboseOption = CreateVerboseOption();
        var forceOption = CreateForceOption();
        var variablesOption = CreateVariablesOption();
        var timeoutOption = CreateTimeoutOption();
        var lastOption = new Option<bool>("--last", "Rollback only the last migration");
        var allOption = new Option<bool>("--all", "Rollback all migrations");

        command.AddOption(connectionStringOption);
        command.AddOption(configOption);
        command.AddOption(upgradesPathOption);
        command.AddOption(downgradesPathOption);
        command.AddOption(verboseOption);
        command.AddOption(forceOption);
        command.AddOption(variablesOption);
        command.AddOption(timeoutOption);
        command.AddOption(lastOption);
        command.AddOption(allOption);

        command.SetHandler(async (context) =>
        {
            var connectionString = context.ParseResult.GetValueForOption(connectionStringOption);
            var config = context.ParseResult.GetValueForOption(configOption);
            var upgradesPath = context.ParseResult.GetValueForOption(upgradesPathOption);
            var downgradesPath = context.ParseResult.GetValueForOption(downgradesPathOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var variables = context.ParseResult.GetValueForOption(variablesOption);
            var timeout = context.ParseResult.GetValueForOption(timeoutOption);
            var rollbackLast = context.ParseResult.GetValueForOption(lastOption);
            var rollbackAll = context.ParseResult.GetValueForOption(allOption);

            var result = await ExecuteWithErrorHandling(async () =>
            {
                var options = BuildCliOptions(connectionString, config, upgradesPath, downgradesPath, verbose, false, force, variables, timeout, false, false);
                var mode = DetermineRollbackMode(rollbackLast, rollbackAll);
                return await ExecuteRollbackAsync(options, mode, context.GetCancellationToken());
            }, context.GetCancellationToken());

            context.ExitCode = result.ExitCode;
        });

        return command;
    }

    private static RollbackMode DetermineRollbackMode(bool rollbackLast, bool rollbackAll)
    {
        if (rollbackLast && rollbackAll)
        {
            throw new ArgumentException("Cannot specify both --last and --all options");
        }

        if (!rollbackLast && !rollbackAll)
        {
            throw new ArgumentException("Must specify either --last or --all option");
        }

        return rollbackLast ? RollbackMode.Last : RollbackMode.All;
    }

    private async Task<CommandResult> ExecuteRollbackAsync(
        CliOptions options, 
        RollbackMode mode, 
        CancellationToken cancellationToken)
    {
        try
        {
            OutputService.WriteInfo($"Starting database rollback ({mode})...");

            // Skip confirmation if force mode is enabled (for automation/scripting)
            if (!options.Force && !await ConfirmRollbackAsync(mode, cancellationToken))
            {
                return CommandResult.UserCancelled();
            }

            var result = await _migrationService.ExecuteRollbackAsync(options, mode, cancellationToken);
            DisplayRollbackResult(result, mode);

            return result.Successful 
                ? CommandResult.Ok("Rollback completed successfully.")
                : CommandResult.MigrationError("Rollback failed", result.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rollback execution failed");
            return CommandResult.MigrationError("Rollback execution failed", ex);
        }
    }

    private async Task<bool> ConfirmRollbackAsync(RollbackMode mode, CancellationToken cancellationToken)
    {
        var message = mode == RollbackMode.Last 
            ? "This will rollback the last migration. Continue?"
            : "This will rollback ALL migrations. Continue?";
            
        return await _userInteractionService.ConfirmActionAsync(message, cancellationToken);
    }

    private void DisplayRollbackResult(DbReactorResult result, RollbackMode mode)
    {
        if (result.Successful)
        {
            OutputService.WriteSuccess($"Rollback ({mode}) completed successfully.");
            
            if (result.Scripts.Any())
            {
                OutputService.WriteInfo($"Rolled back {result.Scripts.Count} migration(s):");
                foreach (var script in result.Scripts)
                {
                    OutputService.WriteInfo($"  • {script.Script.Name}");
                }
            }
        }
        else
        {
            OutputService.WriteError("Rollback failed", result.Error);
        }
    }
}