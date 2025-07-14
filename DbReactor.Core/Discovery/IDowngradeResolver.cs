using DbReactor.Core.Abstractions;
using System.Collections.Generic;

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
        /// <returns>The downgrade script, or null if none found</returns>
        IScript FindDowngradeFor(IScript upgradeScript);

        /// <summary>
        /// Gets all available downgrade scripts
        /// </summary>
        /// <returns>Collection of downgrade scripts</returns>
        IEnumerable<IScript> GetDowngradeScripts();
    }
}