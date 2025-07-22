using System.CommandLine;
using DbReactor.CLI.Configuration;
using DbReactor.CLI.Constants;
using DbReactor.CLI.Models;
using DbReactor.CLI.Services;
using DbReactor.CLI.Services.Validation;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace DbReactor.CLI.Commands;

public class ValidateCommand : BaseCommand
{
    private readonly IConfigurationValidator _validator;

    public ValidateCommand(
        ICliConfigurationService configurationService,
        IOutputService outputService,
        IConfigurationValidator validator,
        ILogger<ValidateCommand> logger) : base(configurationService, outputService, logger)
    {
        _validator = validator;
    }

    public override Command BuildCommand()
    {
        var command = new Command("validate", "Validate configuration and database connection");

        var connectionStringOption = new Option<string>("--connection-string", "Database connection string");
        var upgradesPathOption = CreateUpgradesPathOption();
        var downgradesPathOption = CreateDowngradesPathOption();
        var verboseOption = CreateVerboseOption();

        command.AddOption(connectionStringOption);
        command.AddOption(upgradesPathOption);
        command.AddOption(downgradesPathOption);
        command.AddOption(verboseOption);

        command.SetHandler(HandleCommand, connectionStringOption, upgradesPathOption, downgradesPathOption, verboseOption);

        return command;
    }

    private async Task<int> HandleCommand(
        string connectionString,
        string? upgradesPath,
        string? downgradesPath,
        bool verbose)
    {
        try
        {
            AnsiConsole.MarkupLine("[blue]Validating configuration...[/]");

            var options = BuildCliOptions(connectionString, null, upgradesPath, downgradesPath, verbose, false, false, null, 30, false, false);
            var validationResults = await _validator.ValidateAsync(options);

            DisplayValidationResults(validationResults, verbose);

            bool allValid = validationResults.All(r => r.IsValid);
            if (allValid)
            {
                AnsiConsole.MarkupLine("[green]✓ All validations passed successfully[/]");
                return ExitCodes.Success;
            }
            else
            {
                var errorCount = validationResults.Count(r => !r.IsValid);
                AnsiConsole.MarkupLine($"[red]✗ {errorCount} validation error(s) found[/]");
                return ExitCodes.ValidationError;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Configuration validation failed");
            AnsiConsole.MarkupLine($"[red]✗ Validation failed: {ex.Message}[/]");
            return ExitCodes.ValidationError;
        }
    }

    private static void DisplayValidationResults(IEnumerable<Models.ValidationResult> results, bool verbose)
    {
        var table = new Table();
        table.AddColumn("Component");
        table.AddColumn("Status");
        table.AddColumn("Message");

        foreach (var result in results)
        {
            string statusMarkup = result.Level switch
            {
                ValidationLevel.Success => "[green]✓ Success[/]",
                ValidationLevel.Warning => "[yellow]⚠ Warning[/]",
                ValidationLevel.Error => "[red]✗ Error[/]",
                ValidationLevel.Info => "[blue]ℹ Info[/]",
                _ => "[gray]Unknown[/]"
            };

            // Only show warnings and info in verbose mode, always show success and errors
            if (verbose || result.Level == ValidationLevel.Success || result.Level == ValidationLevel.Error)
            {
                table.AddRow(result.Component, statusMarkup, result.Message);
            }
        }

        AnsiConsole.Write(table);
    }
}