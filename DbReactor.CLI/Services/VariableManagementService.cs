using DbReactor.CLI.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text.Json;

namespace DbReactor.CLI.Services;

public class VariableManagementService : IVariableManagementService
{
    private readonly IVariableEncryptionService _encryptionService;
    private readonly IOutputService _outputService;
    private readonly ILogger<VariableManagementService> _logger;

    // Track sensitive variables during session
    private readonly Dictionary<string, bool> _sessionSensitivityMap = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public VariableManagementService(
        IVariableEncryptionService encryptionService,
        IOutputService outputService,
        ILogger<VariableManagementService> logger)
    {
        _encryptionService = encryptionService;
        _outputService = outputService;
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> GetVariablesAsync(string? configurationPath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            string path = configurationPath ?? GetDefaultConfigPath();

            if (!File.Exists(path))
            {
                return new Dictionary<string, string>();
            }

            string json = await File.ReadAllTextAsync(path, cancellationToken);
            CliOptions? options = JsonSerializer.Deserialize<CliOptions>(json, JsonOptions);
            Dictionary<string, string> variables = options?.Variables ?? new Dictionary<string, string>();
            Dictionary<string, VariableMetadata> metadata = options?.VariableMetadata ?? new Dictionary<string, VariableMetadata>();

            // Update sensitivity map from metadata
            foreach (KeyValuePair<string, VariableMetadata> kvp in metadata)
            {
                _sessionSensitivityMap[kvp.Key] = kvp.Value.IsSensitive;
            }

            // Decrypt sensitive variables for in-memory use
            return DecryptVariables(variables);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load variables from configuration");
            return new Dictionary<string, string>();
        }
    }

    public async Task SaveVariablesAsync(Dictionary<string, string> variables, string? configurationPath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            string path = configurationPath ?? GetDefaultConfigPath();
            CliOptions options;

            // Load existing configuration or create new
            if (File.Exists(path))
            {
                string variablesJson = await File.ReadAllTextAsync(path, cancellationToken);
                options = JsonSerializer.Deserialize<CliOptions>(variablesJson, JsonOptions) ?? new CliOptions();
            }
            else
            {
                options = new CliOptions();

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            // Update variables and metadata
            Dictionary<string, VariableMetadata> metadata = CreateOrUpdateMetadata(variables, options.VariableMetadata);
            options.Variables = EncryptSensitiveVariables(variables);
            options.VariableMetadata = metadata;

            // Save configuration directly
            string json = JsonSerializer.Serialize(options, JsonOptions);
            await File.WriteAllTextAsync(path, json, cancellationToken);

            _logger.LogInformation("Saved {VariableCount} variables to configuration", variables.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save variables to configuration");
            throw;
        }
    }

    public async Task<Dictionary<string, string>> ManageVariablesInteractivelyAsync(Dictionary<string, string>? initialVariables = null, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> variables = new Dictionary<string, string>(initialVariables ?? new Dictionary<string, string>());

        while (true)
        {
            string choice = DisplayCurrentVariablesWithMenu(variables);

            switch (choice)
            {
                case "Add Variable":
                    await AddVariableInteractively(variables);
                    break;

                case "Edit Variable":
                    await EditVariableInteractively(variables);
                    break;

                case "Remove Variable":
                    await RemoveVariableInteractively(variables);
                    break;

                case "Clear All Variables":
                    if (ConfirmClearAllVariables())
                    {
                        variables.Clear();
                        _outputService.WriteSuccess("All variables cleared.");
                    }
                    break;

                case "Save and Continue":
                    return variables;

                case "Cancel":
                    // Return original variables (unchanged)
                    return initialVariables ?? new Dictionary<string, string>();
            }
        }
    }

    public async Task AddVariableAsync(string key, string value, string? configurationPath = null, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> variables = await GetVariablesAsync(configurationPath, cancellationToken);
        variables[key] = value;
        await SaveVariablesAsync(variables, configurationPath, cancellationToken);
    }

    public async Task RemoveVariableAsync(string key, string? configurationPath = null, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> variables = await GetVariablesAsync(configurationPath, cancellationToken);
        if (variables.Remove(key))
        {
            await SaveVariablesAsync(variables, configurationPath, cancellationToken);
        }
    }

    public async Task<bool> HasVariablesAsync(string? configurationPath = null, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> variables = await GetVariablesAsync(configurationPath, cancellationToken);
        return variables.Count > 0;
    }

    private string DisplayCurrentVariablesWithMenu(Dictionary<string, string> variables)
    {
        AnsiConsole.Clear();

        // Create the layout with variables on the left (60%) and menu on the right (40%)
        var layout = new Layout("Root")
            .SplitColumns(
                new Layout("Variables") { Size = 60 },
                new Layout("Menu") { Size = 40 });

        // Create variables table for the left side
        var variablesTable = new Table()
            .Title("[bold blue]Current Variables[/]")
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Key[/]") { Width = 20 })
            .AddColumn(new TableColumn("[bold]Value[/]") { Width = 50, NoWrap = false });

        if (!variables.Any())
        {
            variablesTable.AddRow("[dim]No variables defined[/]", "[dim]--[/]");
        }
        else
        {
            foreach (KeyValuePair<string, string> kvp in variables.OrderBy(v => v.Key))
            {
                // Mask sensitive values based on session tracking
                bool shouldMask = _sessionSensitivityMap.GetValueOrDefault(kvp.Key, false);
                string displayValue = shouldMask ? MaskSensitiveValue(kvp.Value) : kvp.Value;
                
                // Wrap long values to multiple lines
                string wrappedValue = WrapText(displayValue, 50);
                
                variablesTable.AddRow(kvp.Key, wrappedValue);
            }
        }

        // Create menu panel for the right side
        var menuPanel = new Panel(
            new Markup(@"[bold yellow]Variable Management[/]

[green]1.[/] Add Variable
[green]2.[/] Edit Variable  
[green]3.[/] Remove Variable
[green]4.[/] Clear All Variables
[green]5.[/] Save and Continue
[green]6.[/] Cancel

Select an option to manage your variables."))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
            Header = new PanelHeader("[bold]Menu[/]")
        };

        // Set the content for each layout
        layout["Variables"].Update(variablesTable);
        layout["Menu"].Update(menuPanel);

        // Display the layout
        AnsiConsole.Write(layout);
        AnsiConsole.WriteLine();

        // Show the selection prompt
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold yellow]Choose an action:[/]")
                .AddChoices(new[] {
                    "Add Variable",
                    "Edit Variable",
                    "Remove Variable",
                    "Clear All Variables",
                    "Save and Continue",
                    "Cancel"
                }));
    }

    private string WrapText(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxWidth)
            return text;

        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(currentLine))
            {
                currentLine = word;
            }
            else if (currentLine.Length + word.Length + 1 <= maxWidth)
            {
                currentLine += " " + word;
            }
            else
            {
                lines.Add(currentLine);
                currentLine = word;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return string.Join("\n", lines);
    }

    private async Task AddVariableInteractively(Dictionary<string, string> variables)
    {
        string key = AnsiConsole.Ask<string>("Enter variable [green]key[/]:");

        if (string.IsNullOrWhiteSpace(key))
        {
            _outputService.WriteError("Variable key cannot be empty.");
            return;
        }

        bool isSensitive = AnsiConsole.Confirm($"Is [green]{Markup.Escape(key)}[/] a sensitive variable (password, secret, etc.)?", false);

        // Track sensitivity choice for this session
        _sessionSensitivityMap[key] = isSensitive;

        string value = isSensitive
            ? AnsiConsole.Prompt(new TextPrompt<string>("Enter variable [green]value[/]:").Secret())
            : AnsiConsole.Ask<string>("Enter variable [green]value[/]:");

        if (variables.ContainsKey(key))
        {
            bool overwrite = AnsiConsole.Confirm($"Variable [green]{Markup.Escape(key)}[/] already exists. Overwrite?");
            if (!overwrite)
            {
                return;
            }
        }

        variables[key] = value;
        _outputService.WriteSuccess($"Added variable [green]{Markup.Escape(key)}[/]");
    }

    private async Task EditVariableInteractively(Dictionary<string, string> variables)
    {
        if (!variables.Any())
        {
            _outputService.WriteWarning("No variables to edit.");
            return;
        }

        string key = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select variable to edit")
                .AddChoices(variables.Keys.OrderBy(k => k)));

        string currentValue = variables[key];
        bool shouldMask = _sessionSensitivityMap.GetValueOrDefault(key, false);
        string displayValue = shouldMask ? MaskSensitiveValue(currentValue) : currentValue;

        _outputService.WriteInfo($"Current value: {displayValue}");

        bool currentSensitivity = _sessionSensitivityMap.GetValueOrDefault(key, false);
        bool isSensitive = AnsiConsole.Confirm($"Is [green]{Markup.Escape(key)}[/] a sensitive variable?", currentSensitivity);

        // Update sensitivity tracking
        _sessionSensitivityMap[key] = isSensitive;

        string newValue = isSensitive
            ? AnsiConsole.Prompt(new TextPrompt<string>("Enter new [green]value[/]:").Secret())
            : AnsiConsole.Ask<string>("Enter new [green]value[/]:");

        variables[key] = newValue;
        _outputService.WriteSuccess($"Updated variable [green]{Markup.Escape(key)}[/]");
    }

    private async Task RemoveVariableInteractively(Dictionary<string, string> variables)
    {
        if (!variables.Any())
        {
            _outputService.WriteWarning("No variables to remove.");
            return;
        }

        string key = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select variable to remove")
                .AddChoices(variables.Keys.OrderBy(k => k)));

        if (AnsiConsole.Confirm($"Remove variable [green]{Markup.Escape(key)}[/]?"))
        {
            variables.Remove(key);
            _outputService.WriteSuccess($"Removed variable [green]{Markup.Escape(key)}[/]");
        }
    }

    private bool ConfirmClearAllVariables()
    {
        return AnsiConsole.Confirm("Are you sure you want to [red]clear all variables[/]? This cannot be undone.");
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


    private Dictionary<string, VariableMetadata> CreateOrUpdateMetadata(Dictionary<string, string> variables, Dictionary<string, VariableMetadata>? existingMetadata)
    {
        Dictionary<string, VariableMetadata> metadata = existingMetadata ?? new Dictionary<string, VariableMetadata>();

        foreach (KeyValuePair<string, string> kvp in variables)
        {
            bool isSensitive = _sessionSensitivityMap.GetValueOrDefault(kvp.Key, false);

            if (metadata.TryGetValue(kvp.Key, out VariableMetadata? existing))
            {
                // Update existing metadata
                existing.IsSensitive = isSensitive;
                existing.LastModified = DateTime.Now;
            }
            else
            {
                // Create new metadata
                metadata[kvp.Key] = new VariableMetadata
                {
                    IsSensitive = isSensitive,
                    LastModified = DateTime.Now
                };
            }
        }

        // Remove metadata for variables that no longer exist
        List<string> keysToRemove = metadata.Keys.Where(key => !variables.ContainsKey(key)).ToList();
        foreach (string? key in keysToRemove)
        {
            metadata.Remove(key);
        }

        return metadata;
    }

    private Dictionary<string, string> EncryptSensitiveVariables(Dictionary<string, string> variables)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();

        foreach (KeyValuePair<string, string> kvp in variables)
        {
            bool isSensitive = _sessionSensitivityMap.GetValueOrDefault(kvp.Key, false);

            if (isSensitive && !_encryptionService.IsEncrypted(kvp.Value))
            {
                // Encrypt sensitive variables that aren't already encrypted
                result[kvp.Key] = _encryptionService.EncryptValue(kvp.Value);
                _logger.LogDebug("Encrypted variable '{VariableName}' for storage", kvp.Key);
            }
            else
            {
                // Keep non-sensitive or already encrypted variables as-is
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    private Dictionary<string, string> DecryptVariables(Dictionary<string, string> storedVariables)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();

        foreach (KeyValuePair<string, string> kvp in storedVariables)
        {
            if (_encryptionService.IsEncrypted(kvp.Value))
            {
                // Decrypt encrypted variables for in-memory use
                result[kvp.Key] = _encryptionService.DecryptValue(kvp.Value);
                _logger.LogDebug("Decrypted variable '{VariableName}' for use", kvp.Key);
            }
            else
            {
                // Keep plain text variables as-is
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    private static string GetDefaultConfigPath()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "dbreactor.json");
    }
}