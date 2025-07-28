using DbReactor.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Core database reactor engine
    /// </summary>
    public interface IDbReactorEngine
    {
        /// <summary>
        /// Runs the database migration process asynchronously
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The result of the migration process</returns>
        Task<DbReactorResult> RunAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies upgrade migrations asynchronously
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The result of the upgrade process</returns>
        Task<DbReactorResult> ApplyUpgradesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies downgrade migrations asynchronously
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The result of the downgrade process</returns>
        Task<DbReactorResult> ApplyDowngradesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if there are pending upgrades asynchronously
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if there are pending upgrades</returns>
        Task<bool> HasPendingUpgradesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all pending upgrade migrations asynchronously
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Collection of pending migrations</returns>
        Task<IEnumerable<IMigration>> GetPendingUpgradesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all applied upgrade migrations asynchronously
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Collection of applied migrations</returns>
        Task<IEnumerable<IMigration>> GetAppliedUpgradesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes database seeds asynchronously
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The result of the seeding process</returns>
        Task<DbReactorResult> ExecuteSeedsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Previews which seeds would be executed without actually executing them
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Preview result showing which seeds would execute</returns>
        Task<DbReactorSeedPreviewResult> PreviewSeedsAsync(CancellationToken cancellationToken = default);
    }
}