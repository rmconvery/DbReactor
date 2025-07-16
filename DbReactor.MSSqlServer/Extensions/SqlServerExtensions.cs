using DbReactor.Core.Configuration;
using DbReactor.MSSqlServer.Constants;
using DbReactor.MSSqlServer.Execution;
using DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution;
using DbReactor.MSSqlServer.Journaling;
using DbReactor.MSSqlServer.Provisioning;
using System;

namespace DbReactor.MSSqlServer.Extensions
{
    /// <summary>
    /// Extension methods for configuring SQL Server specific implementations
    /// </summary>
    public static class SqlServerExtensions
    {
        #region Complete SQL Server Setup

        /// <summary>
        /// Configures all SQL Server components with sensible defaults
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <param name="commandTimeout">Command timeout (default: 30 seconds)</param>
        /// <param name="journalSchema">Schema for migration journal table (default: dbo)</param>
        /// <param name="journalTable">Migration journal table name (default: __migration_journal)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseSqlServer(this DbReactorConfiguration config, string connectionString, TimeSpan? commandTimeout = null, string journalSchema = SqlServerConstants.Defaults.SchemaName, string journalTable = SqlServerConstants.Defaults.JournalTableName)
        {
            return config
                .UseSqlServerConnection(connectionString)
                .UseSqlServerExecutor(commandTimeout ?? SqlServerConstants.Defaults.CommandTimeout)
                .UseSqlServerJournal(journalSchema, journalTable)
                .UseSqlServerProvisioner(connectionString);
        }

        #endregion

        #region Individual Component Configuration

        /// <summary>
        /// Configures SQL Server connection manager
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseSqlServerConnection(this DbReactorConfiguration config, string connectionString)
        {
            config.ConnectionManager = new SqlServerConnectionManager(connectionString);
            return config;
        }

        /// <summary>
        /// Configures SQL Server script executor
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="commandTimeout">Command timeout (default: 30 seconds)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseSqlServerExecutor(this DbReactorConfiguration config, TimeSpan? commandTimeout = null)
        {
            config.ScriptExecutor = new SqlServerScriptExecutor(commandTimeout ?? SqlServerConstants.Defaults.CommandTimeout);
            return config;
        }

        /// <summary>
        /// Configures SQL Server migration journal for tracking executed migrations
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="schemaName">Schema name for journal table (default: dbo)</param>
        /// <param name="tableName">Journal table name (default: __migration_journal)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseSqlServerJournal(this DbReactorConfiguration config, string schemaName = SqlServerConstants.Defaults.SchemaName, string tableName = SqlServerConstants.Defaults.JournalTableName)
        {
            var journal = new SqlServerScriptJournal(schemaName, tableName);
            if (config.ConnectionManager != null)
            {
                journal.SetConnectionManager(config.ConnectionManager);
            }
            config.MigrationJournal = journal;
            return config;
        }

        /// <summary>
        /// Configures SQL Server database provisioner for creating databases
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseSqlServerProvisioner(this DbReactorConfiguration config, string connectionString)
        {
            config.DatabaseProvisioner = new SqlServerDatabaseProvisioner(connectionString, config.LogProvider);
            return config;
        }

        /// <summary>
        /// Configures the command timeout for SQL Server operations
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="timeout">Command timeout</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseSqlServerCommandTimeout(this DbReactorConfiguration config, TimeSpan timeout)
        {
            // Update the existing executor with the new timeout
            config.ScriptExecutor = new SqlServerScriptExecutor(timeout);
            return config;
        }

        #endregion
    }
}