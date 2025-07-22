using System.CommandLine;
using DbReactor.CLI.Configuration;
using DbReactor.CLI.Models;
using DbReactor.CLI.Services;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Commands;

public abstract class BaseCommand
{
    protected readonly ICliConfigurationService ConfigurationService;
    protected readonly IOutputService OutputService;
    protected readonly ILogger Logger;

    protected BaseCommand(
        ICliConfigurationService configurationService,
        IOutputService outputService,
        ILogger logger)
    {
        ConfigurationService = configurationService;
        OutputService = outputService;
        Logger = logger;
    }

    public abstract Command BuildCommand();

    protected static Option<string> CreateConnectionStringOption() => 
        new("--connection-string", "Database connection string") { IsRequired = true };

    protected static Option<string> CreateConfigOption() => 
        new(new[] { "--config", "-c" }, "Configuration file path");

    protected static Option<string> CreateUpgradesPathOption() => 
        new("--upgrades-path", "Path to upgrades directory");

    protected static Option<string> CreateDowngradesPathOption() => 
        new("--downgrades-path", "Path to downgrades directory");

    protected static Option<bool> CreateVerboseOption() => 
        new(new[] { "--verbose", "-v" }, "Enable verbose logging");

    protected static Option<bool> CreateDryRunOption() => 
        new(new[] { "--dry-run", "-n" }, "Preview changes without executing");

    protected static Option<bool> CreateForceOption() => 
        new(new[] { "--force", "-f" }, "Force execution without confirmation");

    // Allows multiple --variable key=value pairs for script variable substitution
    protected static Option<string[]> CreateVariablesOption() => 
        new(new[] { "--variable", "--var" }, "Variables in key=value format (can be used multiple times)");

    protected static Option<int> CreateTimeoutOption() => 
        new(new[] { "--timeout", "-t" }, () => 30, "Command timeout in seconds");

    protected static Option<bool> CreateEnsureDatabaseOption() => 
        new("--ensure-database", "Create database if it doesn't exist");

    protected static Option<bool> CreateEnsureDirectoriesOption() => 
        new("--ensure-dirs", "Create script directories if they don't exist");

    // Centralizes error handling and logging for all commands
    protected async Task<CommandResult> ExecuteWithErrorHandling(Func<Task<CommandResult>> operation, CancellationToken cancellationToken = default)
    {
        try
        {
            return await operation();
        }
        catch (OperationCanceledException)
        {
            OutputService.WriteWarning("Operation was cancelled");
            return CommandResult.Error("Operation cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Command execution failed");
            OutputService.WriteError("Command execution failed", ex);
            return CommandResult.Error(ex.Message, ex);
        }
    }

    protected CliOptions BuildCliOptions(
        string? connectionString,
        string? config,
        string? upgradesPath,
        string? downgradesPath,
        bool verbose,
        bool dryRun,
        bool force,
        string[]? variables,
        int timeout,
        bool ensureDatabase,
        bool ensureDirectories)
    {
        var options = new CliOptions
        {
            ConnectionString = connectionString,
            ConfigFile = config,
            UpgradesPath = upgradesPath,
            DowngradesPath = downgradesPath,
            Verbose = verbose,
            DryRun = dryRun,
            Force = force,
            TimeoutSeconds = timeout,
            EnsureDatabase = ensureDatabase,
            EnsureDirectories = ensureDirectories,
            LogLevel = verbose ? LogLevel.Debug : LogLevel.Information
        };

        if (variables != null)
        {
            options.Variables = ParseVariables(variables);
        }

        return options;
    }

    // Parses "key=value" strings into dictionary for variable substitution
    private static Dictionary<string, string> ParseVariables(string[] variables)
    {
        var result = new Dictionary<string, string>();

        foreach (var variable in variables)
        {
            var parts = variable.Split('=', 2); // Split only on first '=' to allow '=' in values
            if (parts.Length == 2)
            {
                result[parts[0].Trim()] = parts[1].Trim();
            }
            else
            {
                throw new ArgumentException($"Invalid variable format: '{variable}'. Use key=value format.");
            }
        }

        return result;
    }
}