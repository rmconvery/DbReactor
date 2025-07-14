using System;
using System.Data;

namespace DbReactor.Core.Execution
{
    /// <summary>
    /// Provides database connection and execution capabilities
    /// </summary>
    public interface IConnectionManager
    {
        IDbConnection CreateConnection();
        void ExecuteCommandsWithManagedConnection(Action<Func<IDbCommand>> action);
        T ExecuteCommandsWithManagedConnection<T>(Func<Func<IDbCommand>, T> action);
        
        /// <summary>
        /// Executes an operation with a managed connection and command, ensuring proper disposal
        /// </summary>
        void ExecuteWithManagedConnection(Action<IDbConnection, IDbCommand> operation);
        
        /// <summary>
        /// Executes an operation with a managed connection and command, ensuring proper disposal
        /// </summary>
        T ExecuteWithManagedConnection<T>(Func<IDbConnection, IDbCommand, T> operation);
    }
}
