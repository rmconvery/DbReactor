using DbReactor.CLI.Configuration;
using DbReactor.CLI.Models;
using DbReactor.CLI.Services;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace DbReactor.CLI.Commands;

public class MigrateCommand : BaseCommand
{
    private readonly IMigrationService _migrationService;
    private readonly IUserInteractionService _userInteractionService;

    public MigrateCommand(
        ICliConfigurationService configurationService,
        IOutputService outputService,
        IMigrationService migrationService,
        IUserInteractionService userInteractionService,
        ILogger<MigrateCommand> logger)
        : base(configurationService, outputService, logger)
    {
        _migrationService = migrationService;
        _userInteractionService = userInteractionService;
    }

    public override Command BuildCommand()
    {
        Command command = new Command("migrate", "Run database migrations");

        Option<string> connectionStringOption = CreateConnectionStringOption();
        Option<string> configOption = CreateConfigOption();
        Option<string> upgradesPathOption = CreateUpgradesPathOption();
        Option<string> downgradesPathOption = CreateDowngradesPathOption();
        Option<bool> verboseOption = CreateVerboseOption();
        Option<bool> dryRunOption = CreateDryRunOption();
        Option<bool> forceOption = CreateForceOption();
        Option<string[]> variablesOption = CreateVariablesOption();
        Option<int> timeoutOption = CreateTimeoutOption();
        Option<bool> ensureDatabaseOption = CreateEnsureDatabaseOption();
        Option<bool> ensureDirectoriesOption = CreateEnsureDirectoriesOption();

        command.AddOption(connectionStringOption);
        command.AddOption(configOption);
        command.AddOption(upgradesPathOption);
        command.AddOption(downgradesPathOption);
        command.AddOption(verboseOption);
        command.AddOption(dryRunOption);
        command.AddOption(forceOption);
        command.AddOption(variablesOption);
        command.AddOption(timeoutOption);
        command.AddOption(ensureDatabaseOption);
        command.AddOption(ensureDirectoriesOption);

        command.SetHandler(async (context) =>
        {
            string? connectionString = context.ParseResult.GetValueForOption(connectionStringOption);
            string? config = context.ParseResult.GetValueForOption(configOption);
            string? upgradesPath = context.ParseResult.GetValueForOption(upgradesPathOption);
            string? downgradesPath = context.ParseResult.GetValueForOption(downgradesPathOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            bool force = context.ParseResult.GetValueForOption(forceOption);
            string[]? variables = context.ParseResult.GetValueForOption(variablesOption);
            int timeout = context.ParseResult.GetValueForOption(timeoutOption);
            bool ensureDatabase = context.ParseResult.GetValueForOption(ensureDatabaseOption);
            bool ensureDirectories = context.ParseResult.GetValueForOption(ensureDirectoriesOption);

            CommandResult result = await ExecuteWithErrorHandling(async () =>
            {
                CliOptions options = BuildCliOptions(connectionString, config, upgradesPath, downgradesPath, verbose, dryRun, force, variables, timeout, ensureDatabase, ensureDirectories);
                return await ExecuteMigrationAsync(options, context.GetCancellationToken());
            }, context.GetCancellationToken());

            context.ExitCode = result.ExitCode;
        });

        return command;
    }

    private async Task<CommandResult> ExecuteMigrationAsync(CliOptions options, CancellationToken cancellationToken)
    {
        try
        {
            OutputService.WriteInfo("Starting database migration...");

            if (options.DryRun)
            {
                return await ExecutePreviewAsync(options, cancellationToken);
            }

            if (!options.Force && !await ConfirmMigrationAsync(cancellationToken))
            {
                return CommandResult.UserCancelled();
            }

            Core.Models.DbReactorResult result = await _migrationService.ExecuteMigrationsAsync(options, cancellationToken);
            OutputService.WriteMigrationResult(result);

            return result.Successful
                ? CommandResult.Ok($"Migration completed successfully. Executed {result.Scripts.Count} migrations.")
                : CommandResult.MigrationError("Migration failed", result.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Migration execution failed");
            return CommandResult.MigrationError("Migration execution failed", ex);
        }
    }

    private async Task<CommandResult> ExecutePreviewAsync(CliOptions options, CancellationToken cancellationToken)
    {
        Core.Models.DbReactorPreviewResult preview = await _migrationService.GetMigrationStatusAsync(options, cancellationToken);
        OutputService.WriteInfo($"Preview mode: {preview.PendingMigrations} migrations would be executed");

        var pendingMigrations = preview.MigrationResults.Where(r => !r.AlreadyExecuted);
        foreach (var migration in pendingMigrations)
        {
            OutputService.WriteInfo($"  • {migration.MigrationName}");
        }

        return CommandResult.Ok("Preview completed successfully");
    }

    private async Task<bool> ConfirmMigrationAsync(CancellationToken cancellationToken)
    {
        return await _userInteractionService.ConfirmActionAsync(
            "This will execute database migrations. Continue?",
            cancellationToken);
    }
}