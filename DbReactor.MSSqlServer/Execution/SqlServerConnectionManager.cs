using DbReactor.Core.Execution;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution
{
    /// <summary>
    /// SQL Server async-first implementation of IConnectionManager
    /// </summary>
    public class SqlServerConnectionManager : IConnectionManager
    {
        private readonly string _connectionString;

        public SqlServerConnectionManager(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        public async Task ExecuteWithManagedConnectionAsync(Func<IDbConnection, Task> operation, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await operation(connection);
        }

        public async Task<T> ExecuteWithManagedConnectionAsync<T>(Func<IDbConnection, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return await operation(connection);
        }
    }
}