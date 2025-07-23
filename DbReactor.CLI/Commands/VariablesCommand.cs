using System.CommandLine;
using DbReactor.CLI.Constants;
using DbReactor.CLI.Models;
using DbReactor.CLI.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace DbReactor.CLI.Commands;

public class VariablesCommand : Command
{
    private readonly IVariableManagementService _variableManagementService;
    private readonly IVariableEncryptionService _encryptionService;
    private readonly IOutputService _outputService;
    private readonly ILogger<VariablesCommand> _logger;

    public VariablesCommand(
        IVariableManagementService variableManagementService,
        IVariableEncryptionService encryptionService,
        IOutputService outputService,
        ILogger<VariablesCommand> logger) : base("variables", "Manage configuration variables")
    {
        _variableManagementService = variableManagementService;
        _encryptionService = encryptionService;
        _outputService = outputService;
        _logger = logger;

        AddCommand(CreateListCommand());
        AddCommand(CreateSetCommand());
        AddCommand(CreateRemoveCommand());
        AddCommand(CreateClearCommand());
        AddCommand(CreateManageCommand());
    }

    private Command CreateListCommand()
    {
        var command = new Command("list", "List all configured variables");
        var configOption = new Option<string?>("--config", "Path to configuration file");
        var showValuesOption = new Option<bool>("--show-values", "Show variable values (masked for sensitive variables)");

        command.AddOption(configOption);
        command.AddOption(showValuesOption);

        command.SetHandler(async (context) =>
        {
            var configPath = context.ParseResult.GetValueForOption(configOption);
            var showValues = context.ParseResult.GetValueForOption(showValuesOption);

            try
            {
                var variables = await _variableManagementService.GetVariablesAsync(configPath, context.GetCancellationToken());
                var metadata = await LoadVariableMetadataAsync(configPath, context.GetCancellationToken());

                if (!variables.Any())
                {
                    _outputService.WriteInfo("No variables configured.");
                    context.ExitCode = ExitCodes.Success;
                    return;
                }

                _outputService.WriteSuccess($"Found {variables.Count} variable(s):");
                _outputService.WriteInfo("");

                foreach (var kvp in variables.OrderBy(v => v.Key))
                {
                    if (showValues)
                    {
                        bool isSensitive = metadata.GetValueOrDefault(kvp.Key)?.IsSensitive ?? false;
                        var displayValue = isSensitive ? MaskSensitiveValue(kvp.Value) : kvp.Value;
                        _outputService.WriteInfo($"  {kvp.Key} = {displayValue}");
                    }
                    else
                    {
                        _outputService.WriteInfo($"  {kvp.Key}");
                    }
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list variables");
                _outputService.WriteError($"Failed to list variables: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        return command;
    }

    private Command CreateSetCommand()
    {
        var command = new Command("set", "Set a variable value");
        var keyArgument = new Argument<string>("key", "Variable key");
        var valueArgument = new Argument<string>("value", "Variable value");
        var configOption = new Option<string?>("--config", "Path to configuration file");

        command.AddArgument(keyArgument);
        command.AddArgument(valueArgument);
        command.AddOption(configOption);

        command.SetHandler(async (context) =>
        {
            var key = context.ParseResult.GetValueForArgument(keyArgument);
            var value = context.ParseResult.GetValueForArgument(valueArgument);
            var configPath = context.ParseResult.GetValueForOption(configOption);

            try
            {
                await _variableManagementService.AddVariableAsync(key, value, configPath, context.GetCancellationToken());
                _outputService.WriteSuccess($"✓ Set variable [green]{Markup.Escape(key)}[/]");
                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set variable");
                _outputService.WriteError($"Failed to set variable: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        return command;
    }

    private Command CreateRemoveCommand()
    {
        var command = new Command("remove", "Remove a variable");
        var keyArgument = new Argument<string>("key", "Variable key to remove");
        var configOption = new Option<string?>("--config", "Path to configuration file");

        command.AddArgument(keyArgument);
        command.AddOption(configOption);

        command.SetHandler(async (context) =>
        {
            var key = context.ParseResult.GetValueForArgument(keyArgument);
            var configPath = context.ParseResult.GetValueForOption(configOption);

            try
            {
                await _variableManagementService.RemoveVariableAsync(key, configPath, context.GetCancellationToken());
                _outputService.WriteSuccess($"✓ Removed variable [green]{Markup.Escape(key)}[/]");
                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove variable");
                _outputService.WriteError($"Failed to remove variable: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        return command;
    }

    private Command CreateClearCommand()
    {
        var command = new Command("clear", "Clear all variables");
        var configOption = new Option<string?>("--config", "Path to configuration file");
        var forceOption = new Option<bool>("--force", "Skip confirmation prompt");

        command.AddOption(configOption);
        command.AddOption(forceOption);

        command.SetHandler(async (context) =>
        {
            var configPath = context.ParseResult.GetValueForOption(configOption);
            var force = context.ParseResult.GetValueForOption(forceOption);

            try
            {
                var variables = await _variableManagementService.GetVariablesAsync(configPath, context.GetCancellationToken());

                if (!variables.Any())
                {
                    _outputService.WriteInfo("No variables to clear.");
                    context.ExitCode = ExitCodes.Success;
                    return;
                }

                if (!force)
                {
                    _outputService.WriteWarning($"This will remove all {variables.Count} variable(s). This cannot be undone.");
                    _outputService.WriteInfo("Use --force to skip this confirmation.");
                    context.ExitCode = ExitCodes.UserCancelled;
                    return;
                }

                await _variableManagementService.SaveVariablesAsync(new Dictionary<string, string>(), configPath, context.GetCancellationToken());
                _outputService.WriteSuccess($"✓ Cleared all {variables.Count} variable(s)");
                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear variables");
                _outputService.WriteError($"Failed to clear variables: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        return command;
    }

    private Command CreateManageCommand()
    {
        var command = new Command("manage", "Interactively manage variables");
        var configOption = new Option<string?>("--config", "Path to configuration file");

        command.AddOption(configOption);

        command.SetHandler(async (context) =>
        {
            var configPath = context.ParseResult.GetValueForOption(configOption);

            try
            {
                var currentVariables = await _variableManagementService.GetVariablesAsync(configPath, context.GetCancellationToken());
                var updatedVariables = await _variableManagementService.ManageVariablesInteractivelyAsync(currentVariables, context.GetCancellationToken());

                // Only save if variables were actually changed
                if (!AreVariablesEqual(currentVariables, updatedVariables))
                {
                    await _variableManagementService.SaveVariablesAsync(updatedVariables, configPath, context.GetCancellationToken());
                    _outputService.WriteSuccess($"✓ Saved {updatedVariables.Count} variable(s) to configuration");
                }
                else
                {
                    _outputService.WriteInfo("No changes made to variables.");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to manage variables");
                _outputService.WriteError($"Failed to manage variables: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        return command;
    }

    private async Task<Dictionary<string, VariableMetadata>> LoadVariableMetadataAsync(string? configPath, CancellationToken cancellationToken)
    {
        try
        {
            var path = configPath ?? Path.Combine(Directory.GetCurrentDirectory(), "dbreactor.json");
            
            if (!File.Exists(path))
            {
                return new Dictionary<string, VariableMetadata>();
            }

            var json = await File.ReadAllTextAsync(path, cancellationToken);
            var options = System.Text.Json.JsonSerializer.Deserialize<CliOptions>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            
            return options?.VariableMetadata ?? new Dictionary<string, VariableMetadata>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load variable metadata");
            return new Dictionary<string, VariableMetadata>();
        }
    }

    private string MaskSensitiveValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // If value is encrypted, use special encrypted masking
        if (_encryptionService.IsEncrypted(value))
            return _encryptionService.MaskEncryptedValue(value);

        // Otherwise use plain text masking
        return value.Length <= 4
            ? new string('*', value.Length)
            : value.Substring(0, 2) + new string('*', value.Length - 4) + value.Substring(value.Length - 2);
    }

    private static bool AreVariablesEqual(Dictionary<string, string> dict1, Dictionary<string, string> dict2)
    {
        if (dict1.Count != dict2.Count)
            return false;

        return dict1.All(kvp => dict2.TryGetValue(kvp.Key, out var value) && value == kvp.Value);
    }
}