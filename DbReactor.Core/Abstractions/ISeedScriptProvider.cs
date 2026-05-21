using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Provides seed scripts with strategy already resolved at discovery time.
    /// Unlike IScriptProvider (which returns raw IScript), this provider returns
    /// fully-formed ISeed instances with correct strategy resolution using full path context.
    /// </summary>
    public interface ISeedScriptProvider
    {
        /// <summary>
        /// Gets seeds with strategy resolved at discovery time
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of seeds with strategy already resolved</returns>
        Task<IEnumerable<ISeed>> GetSeedsAsync(CancellationToken cancellationToken = default);
    }
}
