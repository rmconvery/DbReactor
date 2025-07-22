using DbReactor.Core.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Discovery
{
    /// <summary>
    /// Builds migrations by combining upgrade scripts with their corresponding downgrade scripts
    /// </summary>
    public interface IMigrationBuilder
    {
        /// <summary>
        /// Builds a collection of migrations from available scripts
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of migrations</returns>
        Task<IEnumerable<IMigration>> BuildMigrationsAsync(CancellationToken cancellationToken = default);
    }
}