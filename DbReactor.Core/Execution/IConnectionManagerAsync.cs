using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Execution
{
    /// <summary>
    /// Async interface for managing database connections
    /// </summary>
    public interface IConnectionManagerAsync
    {
        /// <summary>
        /// Creates a database connection asynchronously
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A database connection</returns>
        Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes commands with a managed connection asynchronously
        /// </summary>
        /// <param name="commandFactory">Factory function to create commands</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async operation</returns>
        Task ExecuteCommandsWithManagedConnectionAsync(Func<IDbCommand> commandFactory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes commands with a managed connection asynchronously
        /// </summary>
        /// <param name="operation">The operation to execute with the connection</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async operation</returns>
        Task ExecuteWithManagedConnectionAsync(Func<IDbConnection, Task> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes commands with a managed connection asynchronously and returns a result
        /// </summary>
        /// <typeparam name="T">The type of result to return</typeparam>
        /// <param name="operation">The operation to execute with the connection</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The result of the operation</returns>
        Task<T> ExecuteWithManagedConnectionAsync<T>(Func<IDbConnection, Task<T>> operation, CancellationToken cancellationToken = default);
    }
}