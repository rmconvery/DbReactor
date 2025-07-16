using DbReactor.Core.Constants;
using DbReactor.Core.Discovery;
using DbReactor.Core.Enumerations;
using DbReactor.Core.Execution;
using DbReactor.Core.Journaling;
using DbReactor.Core.Logging;
using DbReactor.Core.Provisioning;
using System.Collections.Generic;

namespace DbReactor.Core.Configuration
{
    /// <summary>
    /// Configuration for DbReactor database migration engine
    /// </summary>
    public class DbReactorConfiguration
    {
        #region Core Components
        
        /// <summary>
        /// Database connection manager for executing migrations
        /// </summary>
        public IConnectionManager ConnectionManager { get; set; }

        /// <summary>
        /// Script executor for running SQL commands
        /// </summary>
        public IScriptExecutor ScriptExecutor { get; set; }

        /// <summary>
        /// Migration journal for tracking executed migrations
        /// </summary>
        public IMigrationJournal MigrationJournal { get; set; }

        /// <summary>
        /// Migration builder for creating migration instances from scripts
        /// </summary>
        public IMigrationBuilder MigrationBuilder { get; set; }

        #endregion

        #region Script Discovery

        /// <summary>
        /// Collection of script providers for discovering migration scripts
        /// </summary>
        public List<IScriptProvider> ScriptProviders { get; set; } = new List<IScriptProvider>();

        /// <summary>
        /// Resolver for finding downgrade scripts that correspond to upgrade scripts
        /// </summary>
        public IDowngradeResolver DowngradeResolver { get; set; }

        #endregion

        #region Migration Behavior

        /// <summary>
        /// Order in which scripts should be executed (default: ascending by name)
        /// </summary>
        public ScriptExecutionOrder ExecutionOrder { get; set; } = ScriptExecutionOrder.ByNameAscending;

        /// <summary>
        /// Whether downgrade operations are enabled (default: false)
        /// </summary>
        public bool AllowDowngrades { get; set; } = false;


        #endregion

        #region Database Management

        /// <summary>
        /// Database provisioner for creating databases if they don't exist
        /// </summary>
        public IDatabaseProvisioner DatabaseProvisioner { get; set; }

        /// <summary>
        /// Whether to create the database if it doesn't exist (default: false)
        /// </summary>
        public bool CreateDatabaseIfNotExists { get; set; } = false;

        /// <summary>
        /// SQL template for database creation. Use {0} as placeholder for database name.
        /// </summary>
        public string DatabaseCreationTemplate { get; set; } = null;

        #endregion

        #region Variables and Logging

        /// <summary>
        /// Whether variable substitution in scripts is enabled (default: false)
        /// </summary>
        public bool EnableVariables { get; set; } = false;

        /// <summary>
        /// Variables for substitution in migration scripts
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Log provider for migration execution logging
        /// </summary>
        public ILogProvider LogProvider { get; set; }

        #endregion
    }
}