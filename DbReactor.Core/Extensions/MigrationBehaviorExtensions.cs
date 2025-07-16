using DbReactor.Core.Configuration;
using DbReactor.Core.Enumerations;
using System.Collections.Generic;

namespace DbReactor.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring migration behavior in DbReactor
    /// </summary>
    public static class MigrationBehaviorExtensions
    {
        /// <summary>
        /// Sets the order in which migration scripts are executed
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="order">The execution order to use</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseExecutionOrder(this DbReactorConfiguration config, ScriptExecutionOrder order)
        {
            config.ExecutionOrder = order;
            return config;
        }

        /// <summary>
        /// Executes scripts in ascending order by name (default behavior)
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseAscendingOrder(this DbReactorConfiguration config)
        {
            config.ExecutionOrder = ScriptExecutionOrder.ByNameAscending;
            return config;
        }

        /// <summary>
        /// Executes scripts in descending order by name
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseDescendingOrder(this DbReactorConfiguration config)
        {
            config.ExecutionOrder = ScriptExecutionOrder.ByNameDescending;
            return config;
        }


        /// <summary>
        /// Enables variable substitution in migration scripts
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="variables">Variables to use for substitution</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseVariables(this DbReactorConfiguration config, Dictionary<string, string> variables = null)
        {
            config.EnableVariables = true;
            if (variables != null)
            {
                config.Variables = variables;
            }
            return config;
        }

        /// <summary>
        /// Adds a variable for script substitution
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="name">Variable name</param>
        /// <param name="value">Variable value</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration AddVariable(this DbReactorConfiguration config, string name, string value)
        {
            config.EnableVariables = true;
            config.Variables[name] = value;
            return config;
        }
    }
}