using DbReactor.Core.Abstractions;
using DbReactor.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Execution
{
    /// <summary>
    /// Executes database scripts asynchronously
    /// </summary>
    public interface IScriptExecutor
    {
        /// <summary>
        /// Executes a script asynchronously
        /// </summary>
        /// <param name="script">The script to execute</param>
        /// <param name="connectionManager">The connection manager to use</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The result of the script execution</returns>
        Task<MigrationResult> ExecuteAsync(IScript script, IConnectionManager connectionManager, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies the database schema asynchronously
        /// </summary>
        /// <param name="connectionManager">The connection manager to use</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async operation</returns>
        Task VerifySchemaAsync(IConnectionManager connectionManager, CancellationToken cancellationToken = default);
    }
}
