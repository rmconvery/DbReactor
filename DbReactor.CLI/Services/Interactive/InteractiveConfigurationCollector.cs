using DbReactor.CLI.Models;
using Spectre.Console;

namespace DbReactor.CLI.Services.Interactive;

public class InteractiveConfigurationCollector : IInteractiveConfigurationCollector
{
    public async Task<CliOptions> CollectBaseConfigurationAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Initial Configuration Setup[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Let's collect the basic configuration that will be used for all commands.[/]");
        AnsiConsole.WriteLine();

        CliOptions options = new CliOptions();

        // Connection String (most important)
        options.ConnectionString = CollectConnectionString();

        // Database Provider
        options.Provider = CollectDatabaseProvider();

        // Script Paths
        CollectScriptPaths(options);

        // Common Options
        CollectCommonOptions(options);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓ Configuration collected successfully![/]");
        AnsiConsole.WriteLine();

        return options;
    }

    private string CollectConnectionString()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Database connection string:[/]")
                .PromptStyle("blue")
                .ValidationErrorMessage("[red]Connection string is required[/]")
                .Validate(connectionString =>
                {
                    if (string.IsNullOrWhiteSpace(connectionString))
                        return Spectre.Console.ValidationResult.Error("[red]Connection string cannot be empty[/]");

                    return Spectre.Console.ValidationResult.Success();
                }));
    }

    private string CollectDatabaseProvider()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Database provider:[/]")
                .AddChoices("sqlserver", "mysql", "postgresql", "sqlite")
                .UseConverter(provider => provider switch
                {
                    "sqlserver" => "SQL Server",
                    "mysql" => "MySQL",
                    "postgresql" => "PostgreSQL",
                    "sqlite" => "SQLite",
                    _ => provider
                }));
    }

    private void CollectScriptPaths(CliOptions options)
    {
        AnsiConsole.MarkupLine("[yellow]Script Directory Configuration[/]");

        string upgradesPath = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]Upgrades path (leave empty for default):[/]")
                .DefaultValue("")
                .AllowEmpty());

        if (!string.IsNullOrWhiteSpace(upgradesPath))
        {
            options.UpgradesPath = upgradesPath;
        }

        string downgradesPath = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]Downgrades path (leave empty for default):[/]")
                .DefaultValue("")
                .AllowEmpty());

        if (!string.IsNullOrWhiteSpace(downgradesPath))
        {
            options.DowngradesPath = downgradesPath;
        }

        options.EnsureDirectories = AnsiConsole.Confirm(
            "[yellow]Create directories if they don't exist?[/]", true);
    }

    private void CollectCommonOptions(CliOptions options)
    {
        AnsiConsole.MarkupLine("[yellow]Common Options[/]");

        options.EnsureDatabase = AnsiConsole.Confirm(
            "[yellow]Ensure database exists (create if not found)?[/]", false);

        int timeoutSeconds = AnsiConsole.Prompt(
            new TextPrompt<int>("[blue]Command timeout (seconds):[/]")
                .DefaultValue(30)
                .ValidationErrorMessage("[red]Timeout must be a positive number[/]")
                .Validate(timeout => timeout > 0
                    ? Spectre.Console.ValidationResult.Success()
                    : Spectre.Console.ValidationResult.Error("[red]Timeout must be greater than 0[/]")));

        options.TimeoutSeconds = timeoutSeconds;

        if (AnsiConsole.Confirm("[yellow]Enable verbose logging?[/]", false))
        {
            options.LogLevel = Microsoft.Extensions.Logging.LogLevel.Debug;
        }
    }
}