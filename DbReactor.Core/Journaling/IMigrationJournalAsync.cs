using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Journaling
{
    /// <summary>
    /// Async interface for managing migration journal operations
    /// </summary>
    public interface IMigrationJournalAsync
    {
        /// <summary>
        /// Ensures the journal table exists asynchronously
        /// </summary>
        /// <param name="connectionManager">The connection manager to use</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async operation</returns>
        Task EnsureTableExistsAsync(IConnectionManager connectionManager, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ensures the journal table exists asynchronously
        /// </summary>
        /// <param name="connectionManager">The async connection manager to use</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async operation</returns>
        Task EnsureTableExistsAsync(IConnectionManagerAsync connectionManager, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores an executed migration in the journal asynchronously
        /// </summary>
        /// <param name="migration">The migration that was executed</param>
        /// <param name="result">The result of the migration execution</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async operation</returns>
        Task StoreExecutedMigrationAsync(IMigration migration, MigrationResult result, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an executed migration from the journal asynchronously
        /// </summary>
        /// <param name="upgradeScriptHash">The hash of the upgrade script to remove</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async operation</returns>
        Task RemoveExecutedMigrationAsync(string upgradeScriptHash, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all executed migrations from the journal asynchronously
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Collection of executed migration journal entries</returns>
        Task<IEnumerable<MigrationJournalEntry>> GetExecutedMigrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a migration has been executed asynchronously
        /// </summary>
        /// <param name="migration">The migration to check</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if the migration has been executed</returns>
        Task<bool> HasBeenExecutedAsync(IMigration migration, CancellationToken cancellationToken = default);
    }
}