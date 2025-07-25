using System.CommandLine;
using DbReactor.CLI.Configuration;
using DbReactor.CLI.Constants;
using DbReactor.CLI.Models;
using DbReactor.CLI.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace DbReactor.CLI.Commands;

public class CreateScriptCommand : BaseCommand
{
    private readonly IScriptTemplateService _scriptTemplateService;
    private readonly IDirectoryService _directoryService;

    public CreateScriptCommand(
        IScriptTemplateService scriptTemplateService,
        IDirectoryService directoryService,
        ICliConfigurationService configurationService,
        IOutputService outputService,
        ILogger<CreateScriptCommand> logger) : base(configurationService, outputService, logger)
    {
        _scriptTemplateService = scriptTemplateService;
        _directoryService = directoryService;
    }

    public override Command BuildCommand()
    {
        var command = new Command("create-script", "Create a new migration script from template");

        var nameArgument = new Argument<string>("name", "The name of the migration script");
        var typeOption = new Option<ScriptType>("--type", () => ScriptType.Sql, "Script type (sql only)");
        var upgradesPathOption = new Option<string?>("--upgrades-path", "Path to the upgrades directory");
        var downgradesPathOption = new Option<string?>("--downgrades-path", "Path to the downgrades directory");
        var createDowngradeOption = new Option<bool>("--create-downgrade", () => true, "Create downgrade script (SQL only)");
        var ensureDirectoriesOption = new Option<bool>("--ensure-dirs", () => false, "Create directories if they don't exist");

        command.AddArgument(nameArgument);
        command.AddOption(typeOption);
        command.AddOption(upgradesPathOption);
        command.AddOption(downgradesPathOption);
        command.AddOption(createDowngradeOption);
        command.AddOption(ensureDirectoriesOption);

        command.SetHandler(HandleCommand, nameArgument, typeOption, upgradesPathOption, downgradesPathOption, createDowngradeOption, ensureDirectoriesOption);

        return command;
    }

    private async Task<int> HandleCommand(
        string name,
        ScriptType type,
        string? upgradesPath,
        string? downgradesPath,
        bool createDowngrade,
        bool ensureDirectories)
    {
        try
        {
            AnsiConsole.MarkupLine("[blue]Creating migration script...[/]");

            var paths = _directoryService.DetermineScriptPaths(upgradesPath, downgradesPath, ensureDirectories);
            
            if (ensureDirectories)
            {
                if (!string.IsNullOrEmpty(paths.UpgradesPath))
                    _directoryService.EnsureDirectoryExists(paths.UpgradesPath);
                if (!string.IsNullOrEmpty(paths.DowngradesPath))
                    _directoryService.EnsureDirectoryExists(paths.DowngradesPath);
            }

            var result = await _scriptTemplateService.CreateScriptAsync(
                name, 
                type, 
                paths.UpgradesPath, 
                paths.DowngradesPath, 
                createDowngrade);

            if (result.Success)
            {
                AnsiConsole.MarkupLine($"[green]✓ {result.Message}[/]");
                return ExitCodes.Success;
            }

            AnsiConsole.MarkupLine($"[red]✗ {result.Message}[/]");
            return ExitCodes.GeneralError;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create script: {Name}", name);
            AnsiConsole.MarkupLine($"[red]✗ Failed to create script: {ex.Message}[/]");
            return ExitCodes.GeneralError;
        }
    }
}