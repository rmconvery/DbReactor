using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.Core.Models;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Services;

public class RollbackService : IRollbackService
{
    private readonly ILogger<RollbackService> _logger;

    public RollbackService(ILogger<RollbackService> logger)
    {
        _logger = logger;
    }

    public async Task<DbReactorResult> ExecuteLastMigrationRollbackAsync(DbReactorConfiguration config, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(config);
        
        var engine = new DbReactorEngine(config);
        _logger.LogInformation("Rolling back last migration");
        
        return await engine.ApplyLastDowngradeAsync(cancellationToken);
    }

    public async Task<DbReactorResult> ExecuteAllMigrationsRollbackAsync(DbReactorConfiguration config, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(config);
        
        var engine = new DbReactorEngine(config);
        _logger.LogInformation("Rolling back all migrations");
        
        return await engine.ApplyDowngradesAsync(cancellationToken);
    }

    private static void ValidateConfiguration(DbReactorConfiguration config)
    {
        if (config.DowngradeResolver == null)
        {
            throw new InvalidOperationException("No downgrade resolver configured. Cannot perform rollback.");
        }
    }

}