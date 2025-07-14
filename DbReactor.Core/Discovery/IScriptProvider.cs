using DbReactor.Core.Abstractions;
using System.Collections.Generic;

namespace DbReactor.Core.Discovery
{
    /// <summary>
    /// Discovers upgrade scripts from various sources
    /// </summary>
    public interface IScriptProvider
    {
        /// <summary>
        /// Gets upgrade scripts only
        /// </summary>
        /// <returns>Collection of upgrade scripts</returns>
        IEnumerable<IScript> GetScripts();
    }
}
