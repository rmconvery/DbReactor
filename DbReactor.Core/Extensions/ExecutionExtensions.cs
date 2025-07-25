using DbReactor.Core.Configuration;
using DbReactor.Core.Execution;
using DbReactor.Core.Journaling;
using System;

namespace DbReactor.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring execution components in DbReactor
    /// </summary>
    public static class ExecutionExtensions
    {
        /// <summary>
        /// Sets a custom connection manager for database connections
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="connectionManager">Custom connection manager to use</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration AddConnectionManager(this DbReactorConfiguration config, IConnectionManager connectionManager)
        {
            if (connectionManager == null) throw new ArgumentNullException(nameof(connectionManager));

            config.ConnectionManager = connectionManager;
            return config;
        }

        /// <summary>
        /// Sets a custom script executor for running migration scripts
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="scriptExecutor">Custom script executor to use</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration AddScriptExecutor(this DbReactorConfiguration config, IScriptExecutor scriptExecutor)
        {
            if (scriptExecutor == null) throw new ArgumentNullException(nameof(scriptExecutor));

            config.ScriptExecutor = scriptExecutor;
            return config;
        }

        /// <summary>
        /// Sets a custom migration journal for tracking executed migrations
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="migrationJournal">Custom migration journal to use</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration AddMigrationJournal(this DbReactorConfiguration config, IMigrationJournal migrationJournal)
        {
            if (migrationJournal == null) throw new ArgumentNullException(nameof(migrationJournal));

            config.MigrationJournal = migrationJournal;
            return config;
        }
    }
}