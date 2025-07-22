using System.CommandLine;
using DbReactor.CLI.Configuration;
using DbReactor.CLI.Models;
using DbReactor.CLI.Services;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Commands;

public class StatusCommand : BaseCommand
{
    private readonly IMigrationService _migrationService;

    public StatusCommand(
        ICliConfigurationService configurationService,
        IOutputService outputService,
        IMigrationService migrationService,
        ILogger<StatusCommand> logger) 
        : base(configurationService, outputService, logger)
    {
        _migrationService = migrationService;
    }

    public override Command BuildCommand()
    {
        var command = new Command("status", "Show migration status and history");

        var connectionStringOption = CreateConnectionStringOption();
        var configOption = CreateConfigOption();
        var upgradesPathOption = CreateUpgradesPathOption();
        var downgradesPathOption = CreateDowngradesPathOption();
        var verboseOption = CreateVerboseOption();
        var variablesOption = CreateVariablesOption();
        var timeoutOption = CreateTimeoutOption();

        command.AddOption(connectionStringOption);
        command.AddOption(configOption);
        command.AddOption(upgradesPathOption);
        command.AddOption(downgradesPathOption);
        command.AddOption(verboseOption);
        command.AddOption(variablesOption);
        command.AddOption(timeoutOption);

        command.SetHandler(async (context) =>
        {
            var connectionString = context.ParseResult.GetValueForOption(connectionStringOption);
            var config = context.ParseResult.GetValueForOption(configOption);
            var upgradesPath = context.ParseResult.GetValueForOption(upgradesPathOption);
            var downgradesPath = context.ParseResult.GetValueForOption(downgradesPathOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var variables = context.ParseResult.GetValueForOption(variablesOption);
            var timeout = context.ParseResult.GetValueForOption(timeoutOption);

            var result = await ExecuteWithErrorHandling(async () =>
            {
                var options = BuildCliOptions(connectionString, config, upgradesPath, downgradesPath, verbose, false, false, variables, timeout, false, false);
                return await ShowMigrationStatusAsync(options, context.GetCancellationToken());
            }, context.GetCancellationToken());

            context.ExitCode = result.ExitCode;
        });

        return command;
    }

    private async Task<CommandResult> ShowMigrationStatusAsync(CliOptions options, CancellationToken cancellationToken)
    {
        try
        {
            OutputService.WriteInfo("Retrieving migration status...");
            var preview = await _migrationService.GetMigrationStatusAsync(options, cancellationToken);
            DisplayMigrationStatus(preview);
            return CommandResult.Ok("Status retrieved successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retrieve migration status");
            return CommandResult.Error("Failed to retrieve migration status", ex);
        }
    }

    private void DisplayMigrationStatus(Core.Models.DbReactorPreviewResult preview)
    {
        var migrations = CombineAndSortMigrations(preview);
        
        if (!migrations.Any())
        {
            OutputService.WriteInfo("No migrations found");
            return;
        }

        OutputService.WriteTable(migrations, "Migration Status");
        DisplaySummary(preview);
    }

    private static List<MigrationStatusInfo> CombineAndSortMigrations(Core.Models.DbReactorPreviewResult preview)
    {
        var migrations = new List<MigrationStatusInfo>();
        
        migrations.AddRange(preview.MigrationResults
            .Where(r => r.AlreadyExecuted)
            .Select(r => new MigrationStatusInfo
            {
                Name = r.MigrationName,
                Status = "Executed",
                Type = GetMigrationType(r.MigrationName)
            }));

        migrations.AddRange(preview.MigrationResults
            .Where(r => !r.AlreadyExecuted)
            .Select(r => new MigrationStatusInfo
            {
                Name = r.MigrationName,
                Status = "Pending",
                Type = GetMigrationType(r.MigrationName)
            }));

        return migrations.OrderBy(m => m.Name).ToList();
    }

    private void DisplaySummary(Core.Models.DbReactorPreviewResult preview)
    {
        var totalCount = preview.TotalMigrations;
        var summary = $"Total: {totalCount} migrations " +
                     $"({preview.SkippedMigrations} executed, {preview.PendingMigrations} pending)";
        OutputService.WriteInfo(summary);
    }

    private static string GetMigrationType(string migrationName)
    {
        if (migrationName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            return "SQL";
        if (migrationName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return "C# Code";
        return "Unknown";
    }
}