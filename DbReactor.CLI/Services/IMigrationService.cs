using DbReactor.CLI.Models;
using DbReactor.Core.Models;

namespace DbReactor.CLI.Services;

public interface IMigrationService
{
    Task<DbReactorResult> ExecuteMigrationsAsync(CliOptions options, CancellationToken cancellationToken = default);
    Task<DbReactorResult> ExecuteRollbackAsync(CliOptions options, RollbackMode mode, CancellationToken cancellationToken = default);
    Task<DbReactorPreviewResult> GetMigrationStatusAsync(CliOptions options, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(CliOptions options, CancellationToken cancellationToken = default);
}