namespace DbReactor.MSSqlServer.Execution
{
    using global::DbReactor.Core.Execution;
    using Microsoft.Data.SqlClient;
    using System;
    using System.Data;

    namespace DbReactor.MSSqlServer.Implementations.Execution
    {
        /// <summary>
        /// SQL Server implementation of IConnectionManager
        /// </summary>
        public class SqlServerConnectionManager : IConnectionManager
        {
            private readonly string _connectionString;

            public SqlServerConnectionManager(string connectionString)
            {
                _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            }

            public IDbConnection CreateConnection()
            {
                return new SqlConnection(_connectionString);
            }

            public void ExecuteCommandsWithManagedConnection(Action<Func<IDbCommand>> action)
            {
                using (IDbConnection connection = CreateConnection())
                {
                    connection.Open();
                    action(() => {
                        var command = connection.CreateCommand();
                        // Note: Commands created this way should be disposed by the caller
                        // Consider using ExecuteWithManagedConnection for automatic disposal
                        return command;
                    });
                }
            }

            public T ExecuteCommandsWithManagedConnection<T>(Func<Func<IDbCommand>, T> action)
            {
                using (IDbConnection connection = CreateConnection())
                {
                    connection.Open();
                    return action(() => {
                        var command = connection.CreateCommand();
                        // Note: Commands created this way should be disposed by the caller
                        // Consider using ExecuteWithManagedConnection for automatic disposal
                        return command;
                    });
                }
            }

            /// <summary>
            /// Executes an operation with a managed connection and command, ensuring proper disposal
            /// </summary>
            public void ExecuteWithManagedConnection(Action<IDbConnection, IDbCommand> operation)
            {
                using (IDbConnection connection = CreateConnection())
                {
                    connection.Open();
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        operation(connection, command);
                    }
                }
            }

            /// <summary>
            /// Executes an operation with a managed connection and command, ensuring proper disposal
            /// </summary>
            public T ExecuteWithManagedConnection<T>(Func<IDbConnection, IDbCommand, T> operation)
            {
                using (IDbConnection connection = CreateConnection())
                {
                    connection.Open();
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        return operation(connection, command);
                    }
                }
            }
        }
    }

}
