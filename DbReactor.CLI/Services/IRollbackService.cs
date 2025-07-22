using DbReactor.CLI.Models;
using DbReactor.Core.Configuration;
using DbReactor.Core.Models;

namespace DbReactor.CLI.Services;

public interface IRollbackService
{
    Task<DbReactorResult> ExecuteLastMigrationRollbackAsync(DbReactorConfiguration config, CancellationToken cancellationToken = default);
    Task<DbReactorResult> ExecuteAllMigrationsRollbackAsync(DbReactorConfiguration config, CancellationToken cancellationToken = default);
}