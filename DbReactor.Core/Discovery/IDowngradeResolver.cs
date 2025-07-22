using DbReactor.Core.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Discovery
{
    /// <summary>
    /// Resolves downgrade scripts for upgrade scripts
    /// </summary>
    public interface IDowngradeResolver
    {
        /// <summary>
        /// Finds the corresponding downgrade script for an upgrade script
        /// </summary>
        /// <param name="upgradeScript">The upgrade script to find a downgrade for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The downgrade script, or null if none found</returns>
        Task<IScript> FindDowngradeForAsync(IScript upgradeScript, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available downgrade scripts
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of downgrade scripts</returns>
        Task<IEnumerable<IScript>> GetDowngradeScriptsAsync(CancellationToken cancellationToken = default);
    }
}