using DbReactor.Core.Configuration;
using DbReactor.MSSqlServer.Execution;
using DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution;
using DbReactor.MSSqlServer.Journaling;
using DbReactor.MSSqlServer.Provisioning;

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
        /// <param name="commandTimeoutSeconds">Command timeout in seconds (default: 30)</param>
        /// <param name="journalSchema">Schema for migration journal table (default: dbo)</param>
        /// <param name="journalTable">Migration journal table name (default: MigrationJournal)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseSqlServer(this DbReactorConfiguration config, string connectionString, int commandTimeoutSeconds = 30, string journalSchema = "dbo", string journalTable = "MigrationJournal")
        {
            return config
                .UseSqlServerConnection(connectionString)
                .UseSqlServerExecutor(commandTimeoutSeconds)
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
        /// <param name="commandTimeoutSeconds">Command timeout in seconds (default: 30)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseSqlServerExecutor(this DbReactorConfiguration config, int commandTimeoutSeconds = 30)
        {
            config.ScriptExecutor = new SqlServerScriptExecutor(commandTimeoutSeconds);
            return config;
        }

        /// <summary>
        /// Configures SQL Server migration journal for tracking executed migrations
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="schemaName">Schema name for journal table (default: dbo)</param>
        /// <param name="tableName">Journal table name (default: MigrationJournal)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseSqlServerJournal(this DbReactorConfiguration config, string schemaName = "dbo", string tableName = "MigrationJournal")
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

        #endregion
    }
}