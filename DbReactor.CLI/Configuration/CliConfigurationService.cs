using System.Text.Json;
using DbReactor.CLI.Models;
using DbReactor.CLI.Services;
using DbReactor.Core.Configuration;
using DbReactor.Core.Extensions;
using DbReactor.MSSqlServer.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Configuration;

public class CliConfigurationService : ICliConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CliConfigurationService> _logger;
    private readonly IProviderConfigurationFactory _providerFactory;
    private readonly IDirectoryService _directoryService;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CliConfigurationService(
        IConfiguration configuration, 
        ILogger<CliConfigurationService> logger,
        IProviderConfigurationFactory providerFactory,
        IDirectoryService directoryService)
    {
        _configuration = configuration;
        _logger = logger;
        _providerFactory = providerFactory;
        _directoryService = directoryService;
    }

    public async Task<DbReactorConfiguration> BuildConfigurationAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        ValidateOptions(options);
        
        var config = new DbReactorConfiguration();
        
        ConfigureProvider(config, options);
        ConfigureScriptDiscovery(config, options);
        ConfigureDatabaseOptions(config, options);
        ConfigureLogging(config, options);
        ConfigureVariables(config, options);

        return config;
    }

    public CliOptions GetDefaultOptions() => new()
    {
        Provider = "sqlserver",
        LogLevel = LogLevel.Information,
        TimeoutSeconds = 30
    };

    public async Task<bool> SaveConfigurationAsync(string configPath, CliOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(options, JsonOptions);
            await File.WriteAllTextAsync(configPath, json, cancellationToken);
            _logger.LogInformation("Configuration saved to {ConfigPath}", configPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {ConfigPath}", configPath);
            return false;
        }
    }

    public async Task<CliOptions> LoadConfigurationAsync(string? configPath = null, CancellationToken cancellationToken = default)
    {
        configPath ??= GetDefaultConfigPath();

        if (!File.Exists(configPath))
        {
            _logger.LogDebug("Configuration file not found at {ConfigPath}, using defaults", configPath);
            return GetDefaultOptions();
        }

        return await LoadFromFile(configPath, cancellationToken);
    }

    private static void ValidateOptions(CliOptions options)
    {
        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new ArgumentException("Connection string is required", nameof(options));
        }
    }

    private void ConfigureProvider(DbReactorConfiguration config, CliOptions options)
    {
        _providerFactory.ConfigureProvider(config, options.Provider!, options.ConnectionString!);
    }

    private void ConfigureScriptDiscovery(DbReactorConfiguration config, CliOptions options)
    {
        var (upgradesPath, downgradesPath) = _directoryService.DetermineScriptPaths(
            options.UpgradesPath, 
            options.DowngradesPath, 
            options.EnsureDirectories);
        
        // Always configure at least one script provider - use current directory if no upgrades path
        var scriptPath = !string.IsNullOrEmpty(upgradesPath) 
            ? upgradesPath 
            : Directory.GetCurrentDirectory();
        
        config.UseFileSystemScripts(scriptPath);

        // Configure file system downgrade resolver if downgrade path is specified
        if (!string.IsNullOrEmpty(downgradesPath))
        {
            config.UseFileSystemDowngrades(downgradesPath);
        }
    }

    private static void ConfigureDatabaseOptions(DbReactorConfiguration config, CliOptions options)
    {
        if (options.EnsureDatabase)
        {
            config.CreateDatabaseIfNotExists = true;
        }

        // Command timeout will be set through the SQL Server configuration
        // The timeout is passed to UseSqlServer() method in the provider factory
    }

    private static void ConfigureLogging(DbReactorConfiguration config, CliOptions options)
    {
        config.UseConsoleLogging();
    }

    private static void ConfigureVariables(DbReactorConfiguration config, CliOptions options)
    {
        // Add all user-specified variables to configuration
        foreach (var variable in options.Variables)
        {
            config.AddVariable(variable.Key, variable.Value);
        }
    }

    private static string GetDefaultConfigPath() => 
        Path.Combine(Directory.GetCurrentDirectory(), "dbreactor.json");

    private async Task<CliOptions> LoadFromFile(string configPath, CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(configPath, cancellationToken);
            var options = JsonSerializer.Deserialize<CliOptions>(json, JsonOptions) ?? GetDefaultOptions();
            _logger.LogDebug("Configuration loaded from {ConfigPath}", configPath);
            return options;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load configuration from {ConfigPath}, using defaults", configPath);
            return GetDefaultOptions();
        }
    }
}