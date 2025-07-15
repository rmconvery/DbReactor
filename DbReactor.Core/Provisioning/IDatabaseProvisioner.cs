using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Provisioning
{
    /// <summary>
    /// Provides async-first database creation and existence checking capabilities
    /// </summary>
    public interface IDatabaseProvisioner
    {
        /// <summary>
        /// Asynchronously checks if the target database exists
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if database exists, false otherwise</returns>
        Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously creates the target database
        /// </summary>
        /// <param name="template">Optional SQL template for database creation. Use {0} as placeholder for database name.</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        Task CreateDatabaseAsync(string template = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously ensures the target database exists, creating it if necessary
        /// </summary>
        /// <param name="template">Optional SQL template for database creation. Use {0} as placeholder for database name.</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        Task EnsureDatabaseExistsAsync(string template = null, CancellationToken cancellationToken = default);
    }
}