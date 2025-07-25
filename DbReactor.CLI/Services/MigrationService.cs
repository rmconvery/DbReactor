using DbReactor.CLI.Configuration;
using DbReactor.CLI.Models;
using DbReactor.Core.Engine;
using DbReactor.Core.Models;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Services;

public class MigrationService : IMigrationService
{
    private readonly ICliConfigurationService _configurationService;
    private readonly IRollbackService _rollbackService;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(
        ICliConfigurationService configurationService,
        IRollbackService rollbackService,
        ILogger<MigrationService> logger)
    {
        _configurationService = configurationService;
        _rollbackService = rollbackService;
        _logger = logger;
    }

    public async Task<DbReactorResult> ExecuteMigrationsAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        var config = await _configurationService.BuildConfigurationAsync(options, cancellationToken);
        var engine = new DbReactorEngine(config);
        
        _logger.LogInformation("Executing migrations");
        return await engine.RunAsync(cancellationToken);
    }

    public async Task<DbReactorPreviewResult> GetMigrationStatusAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        var config = await _configurationService.BuildConfigurationAsync(options, cancellationToken);
        var engine = new DbReactorEngine(config);
        
        _logger.LogInformation("Retrieving migration status");
        return await engine.RunPreviewAsync(cancellationToken);
    }

    public async Task<DbReactorResult> ExecuteRollbackAsync(CliOptions options, RollbackMode mode, CancellationToken cancellationToken = default)
    {
        var config = await _configurationService.BuildConfigurationAsync(options, cancellationToken);
        
        // Enable downgrades for rollback operations
        config.AllowDowngrades = true;
        
        _logger.LogInformation("Executing rollback in mode: {Mode}", mode);

        return mode switch
        {
            RollbackMode.Last => await _rollbackService.ExecuteLastMigrationRollbackAsync(config, cancellationToken),
            RollbackMode.All => await _rollbackService.ExecuteAllMigrationsRollbackAsync(config, cancellationToken),
            _ => throw new ArgumentException($"Unsupported rollback mode: {mode}")
        };
    }

    public async Task<bool> ValidateConfigurationAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await _configurationService.BuildConfigurationAsync(options, cancellationToken);
            _logger.LogInformation("Configuration validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed");
            return false;
        }
    }
}