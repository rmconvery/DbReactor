using DbReactor.Core.Execution;
using System.Collections.Generic;

namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Base class for code scripts that provides default implementations for variable-aware methods
    /// </summary>
    public abstract class CodeScriptBase : ICodeScript
    {
        /// <summary>
        /// Generates the SQL for the upgrade operation
        /// </summary>
        /// <param name="connectionManager">Database connection manager for querying data</param>
        /// <returns>SQL script to execute for upgrade</returns>
        public abstract string GetUpgradeScript(IConnectionManager connectionManager);

        /// <summary>
        /// Generates the SQL for the upgrade operation with variable substitution
        /// Default implementation calls the non-variable version for backward compatibility
        /// </summary>
        /// <param name="connectionManager">Database connection manager for querying data</param>
        /// <param name="variables">Variables available for use in script generation</param>
        /// <returns>SQL script to execute for upgrade</returns>
        public virtual string GetUpgradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables)
        {
            return GetUpgradeScript(connectionManager);
        }

        /// <summary>
        /// Generates the SQL for the downgrade operation (optional)
        /// </summary>
        /// <param name="connectionManager">Database connection manager for querying data</param>
        /// <returns>SQL script to execute for downgrade, or null if not supported</returns>
        public abstract string GetDowngradeScript(IConnectionManager connectionManager);

        /// <summary>
        /// Generates the SQL for the downgrade operation with variable substitution (optional)
        /// Default implementation calls the non-variable version for backward compatibility
        /// </summary>
        /// <param name="connectionManager">Database connection manager for querying data</param>
        /// <param name="variables">Variables available for use in script generation</param>
        /// <returns>SQL script to execute for downgrade, or null if not supported</returns>
        public virtual string GetDowngradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables)
        {
            return GetDowngradeScript(connectionManager);
        }

        /// <summary>
        /// Indicates whether this script supports downgrade operations
        /// </summary>
        public abstract bool SupportsDowngrade { get; }
    }
}