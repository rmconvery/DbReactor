using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Tracks seed execution history
    /// </summary>
    public interface ISeedJournal
    {
        /// <summary>
        /// Ensures the seed journal table exists
        /// </summary>
        /// <param name="connectionManager">Connection manager</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task EnsureTableExistsAsync(IConnectionManager connectionManager, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a seed has been executed
        /// </summary>
        /// <param name="seed">The seed to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the seed has been executed</returns>
        Task<bool> HasBeenExecutedAsync(ISeed seed, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the last executed hash for a seed
        /// </summary>
        /// <param name="seedName">Name of the seed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Hash of the last executed version, or null if never executed</returns>
        Task<string> GetLastExecutedHashAsync(string seedName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a seed execution
        /// </summary>
        /// <param name="seed">The executed seed</param>
        /// <param name="executedOn">When the seed was executed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RecordExecutionAsync(ISeed seed, DateTime executedOn, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all executed seeds
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of executed seed entries</returns>
        Task<IEnumerable<SeedJournalEntry>> GetExecutedSeedsAsync(CancellationToken cancellationToken = default);
    }
}