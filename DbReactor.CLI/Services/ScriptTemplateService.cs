using DbReactor.CLI.Models;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Services;

public class ScriptTemplateService : IScriptTemplateService
{
    private readonly IDirectoryService _directoryService;
    private readonly ITemplateService _templateService;
    private readonly ILogger<ScriptTemplateService> _logger;

    public ScriptTemplateService(
        IDirectoryService directoryService,
        ITemplateService templateService, 
        ILogger<ScriptTemplateService> logger)
    {
        _directoryService = directoryService;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<CommandResult> CreateScriptAsync(
        string name, 
        ScriptType type, 
        string? upgradesPath, 
        string? downgradesPath,
        bool createDowngrade,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateScriptName(name);
            ValidatePaths(upgradesPath, downgradesPath, createDowngrade);
            
            await CreateScriptFiles(name, type, upgradesPath!, downgradesPath, createDowngrade, cancellationToken);

            var message = DetermineSuccessMessage(name, type, createDowngrade);
            return CommandResult.Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create script: {Name}", name);
            return CommandResult.Error($"Failed to create script: {ex.Message}", ex);
        }
    }

    private async Task CreateScriptFiles(
        string name, 
        ScriptType type, 
        string upgradesPath, 
        string? downgradesPath, 
        bool createDowngrade,
        CancellationToken cancellationToken)
    {
        switch (type)
        {
            case ScriptType.Sql:
                await CreateSqlScripts(name, upgradesPath, downgradesPath, createDowngrade, cancellationToken);
                break;
                
            default:
                throw new ArgumentException($"Unsupported script type: {type}");
        }
    }

    private async Task CreateSqlScripts(
        string name, 
        string upgradesPath, 
        string? downgradesPath, 
        bool createDowngrade,
        CancellationToken cancellationToken)
    {
        // Create upgrade SQL script
        await CreateSqlScript(name, upgradesPath, "SqlUpgrade.template", cancellationToken);

        // Create downgrade SQL script if requested
        if (createDowngrade && !string.IsNullOrEmpty(downgradesPath))
        {
            await CreateSqlScript(name, downgradesPath, "SqlDowngrade.template", cancellationToken);
        }
    }

    private async Task CreateSqlScript(
        string name, 
        string targetPath, 
        string templateName, 
        CancellationToken cancellationToken)
    {
        _directoryService.EnsureDirectoryExists(targetPath);
        
        var template = await _templateService.LoadTemplateAsync(templateName, cancellationToken);
        var variables = CreateTemplateVariables(name);
        var content = _templateService.RenderTemplate(template, variables);
        
        var fileName = GetSqlFileName(name);
        var filePath = Path.Combine(targetPath, fileName);
        
        await WriteScriptFile(filePath, content, cancellationToken);
        _logger.LogInformation("Created SQL script: {FilePath}", filePath);
    }


    private static Dictionary<string, string> CreateTemplateVariables(string name)
    {
        return new Dictionary<string, string>
        {
            ["Name"] = name,
            ["CreatedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }


    private static void ValidateScriptName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Script name cannot be empty");
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0)
        {
            throw new ArgumentException("Script name contains invalid characters");
        }
    }

    private static void ValidatePaths(string? upgradesPath, string? downgradesPath, bool createDowngrade)
    {
        if (string.IsNullOrEmpty(upgradesPath))
        {
            throw new ArgumentException("Upgrades path is required");
        }

        if (createDowngrade && string.IsNullOrEmpty(downgradesPath))
        {
            throw new ArgumentException("Downgrades path is required when creating downgrade scripts");
        }
    }

    private static async Task WriteScriptFile(string filePath, string content, CancellationToken cancellationToken)
    {
        if (File.Exists(filePath))
        {
            throw new InvalidOperationException($"Script file already exists: {filePath}");
        }

        await File.WriteAllTextAsync(filePath, content, cancellationToken);
    }

    private static string GetSqlFileName(string name)
    {
        return name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) ? name : name + ".sql";
    }


    private static string DetermineSuccessMessage(string name, ScriptType type, bool createDowngrade)
    {
        var scriptType = type.ToString().ToUpperInvariant();
        
        return createDowngrade 
            ? $"Created {scriptType} upgrade and downgrade scripts: {name}"
            : $"Created {scriptType} upgrade script: {name}";
    }
}