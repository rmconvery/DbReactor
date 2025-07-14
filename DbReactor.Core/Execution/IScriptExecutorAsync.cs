using DbReactor.Core.Abstractions;
using DbReactor.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Execution
{
    /// <summary>
    /// Async interface for executing database scripts
    /// </summary>
    public interface IScriptExecutorAsync
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
        /// Executes a script asynchronously with async connection manager
        /// </summary>
        /// <param name="script">The script to execute</param>
        /// <param name="connectionManager">The async connection manager to use</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The result of the script execution</returns>
        Task<MigrationResult> ExecuteAsync(IScript script, IConnectionManagerAsync connectionManager, CancellationToken cancellationToken = default);
    }
}