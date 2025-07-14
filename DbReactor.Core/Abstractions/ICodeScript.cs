using DbReactor.Core.Execution;
using System.Collections.Generic;

namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Interface for code-based migration scripts that generate SQL for upgrade and optional downgrade operations
    /// </summary>
    public interface ICodeScript
    {
        /// <summary>
        /// Generates the SQL for the upgrade operation
        /// </summary>
        /// <param name="connectionManager">Database connection manager for querying data</param>
        /// <returns>SQL script to execute for upgrade</returns>
        string GetUpgradeScript(IConnectionManager connectionManager);

        /// <summary>
        /// Generates the SQL for the upgrade operation with variable substitution
        /// </summary>
        /// <param name="connectionManager">Database connection manager for querying data</param>
        /// <param name="variables">Variables available for use in script generation</param>
        /// <returns>SQL script to execute for upgrade</returns>
        string GetUpgradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables);

        /// <summary>
        /// Generates the SQL for the downgrade operation (optional)
        /// </summary>
        /// <param name="connectionManager">Database connection manager for querying data</param>
        /// <returns>SQL script to execute for downgrade, or null if not supported</returns>
        string GetDowngradeScript(IConnectionManager connectionManager);

        /// <summary>
        /// Generates the SQL for the downgrade operation with variable substitution (optional)
        /// </summary>
        /// <param name="connectionManager">Database connection manager for querying data</param>
        /// <param name="variables">Variables available for use in script generation</param>
        /// <returns>SQL script to execute for downgrade, or null if not supported</returns>
        string GetDowngradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables);

        /// <summary>
        /// Indicates whether this script supports downgrade operations
        /// </summary>
        bool SupportsDowngrade { get; }
    }
}