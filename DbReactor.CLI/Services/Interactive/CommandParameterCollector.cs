using Spectre.Console;

namespace DbReactor.CLI.Services.Interactive;

using DbReactor.CLI.Models;

public class CommandParameterCollector : ICommandParameterCollector
{
    public async Task<string[]> CollectParametersAsync(string commandName, CliOptions baseConfiguration)
    {
        return commandName switch
        {
            "migrate" => BuildMigrateParameters(baseConfiguration),
            "status" => BuildStatusParameters(baseConfiguration),
            "rollback" => await CollectRollbackParametersAsync(baseConfiguration),
            "create-script" => await CollectCreateScriptParametersAsync(),
            "validate" => BuildValidateParameters(baseConfiguration),
            "exit" => Array.Empty<string>(),
            _ => throw new ArgumentException($"Unknown command: {commandName}")
        };
    }

    private string[] BuildMigrateParameters(CliOptions baseConfiguration)
    {
        var args = new List<string>();

        // Use base configuration
        args.AddRange(new[] { "--connection-string", baseConfiguration.ConnectionString });
        
        if (!string.IsNullOrEmpty(baseConfiguration.UpgradesPath))
            args.AddRange(new[] { "--upgrades-path", baseConfiguration.UpgradesPath });
        
        if (!string.IsNullOrEmpty(baseConfiguration.DowngradesPath))
            args.AddRange(new[] { "--downgrades-path", baseConfiguration.DowngradesPath });

        if (baseConfiguration.EnsureDatabase)
            args.Add("--ensure-database");
        
        if (baseConfiguration.EnsureDirectories)
            args.Add("--ensure-dirs");

        // Ask for command-specific options
        if (AnsiConsole.Confirm("[yellow]Dry run (preview only)?[/]", false))
            args.Add("--dry-run");

        return args.ToArray();
    }

    private string[] BuildStatusParameters(CliOptions baseConfiguration)
    {
        var args = new List<string>();
        args.AddRange(new[] { "--connection-string", baseConfiguration.ConnectionString });
        
        if (baseConfiguration.LogLevel == Microsoft.Extensions.Logging.LogLevel.Debug)
            args.Add("--verbose");

        return args.ToArray();
    }

    private async Task<string[]> CollectRollbackParametersAsync(CliOptions baseConfiguration)
    {
        var args = new List<string>();

        // Use base configuration
        args.AddRange(new[] { "--connection-string", baseConfiguration.ConnectionString });

        if (!string.IsNullOrEmpty(baseConfiguration.DowngradesPath))
            args.AddRange(new[] { "--downgrades-path", baseConfiguration.DowngradesPath });

        // Ask for rollback-specific options
        var rollbackType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Rollback type:[/]")
                .AddChoices("last", "all"));

        args.Add($"--{rollbackType}");

        return args.ToArray();
    }


    private async Task<string[]> CollectCreateScriptParametersAsync()
    {
        var args = new List<string>();

        var scriptName = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Script name:[/]"));

        args.Add(scriptName);

        // Currently only SQL is supported, but structured for future extensibility
        var scriptType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Script type:[/]")
                .AddChoices("sql")
                .UseConverter(type => type switch
                {
                    "sql" => "SQL Script",
                    _ => type
                }));

        args.AddRange(new[] { "--type", scriptType });

        var outputPath = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]Output path (leave empty for current directory):[/]")
                .DefaultValue("")
                .AllowEmpty());

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            args.AddRange(new[] { "--output", outputPath });
        }

        return args.ToArray();
    }

    private string[] BuildValidateParameters(CliOptions baseConfiguration)
    {
        var args = new List<string>();

        args.AddRange(new[] { "--connection-string", baseConfiguration.ConnectionString });
        
        if (!string.IsNullOrEmpty(baseConfiguration.UpgradesPath))
            args.AddRange(new[] { "--upgrades-path", baseConfiguration.UpgradesPath });
        
        if (!string.IsNullOrEmpty(baseConfiguration.DowngradesPath))
            args.AddRange(new[] { "--downgrades-path", baseConfiguration.DowngradesPath });

        if (baseConfiguration.LogLevel == Microsoft.Extensions.Logging.LogLevel.Debug)
            args.Add("--verbose");

        return args.ToArray();
    }

}