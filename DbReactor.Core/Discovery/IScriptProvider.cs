using DbReactor.Core.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of upgrade scripts</returns>
        Task<IEnumerable<IScript>> GetScriptsAsync(CancellationToken cancellationToken = default);
    }
}
