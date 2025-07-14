namespace DbReactor.Core.Constants
{
    /// <summary>
    /// Constants used throughout the DbReactor system
    /// </summary>
    public static class DbReactorConstants
    {
        /// <summary>
        /// Default values for configuration
        /// </summary>
        public static class Defaults
        {
            /// <summary>
            /// Default command timeout in seconds
            /// </summary>
            public const int CommandTimeoutSeconds = 30;

            /// <summary>
            /// Default journal table name
            /// </summary>
            public const string JournalTableName = "MigrationJournal";

            /// <summary>
            /// Default schema name for SQL Server
            /// </summary>
            public const string DefaultSchemaName = "dbo";
        }

        /// <summary>
        /// File extensions supported by the system
        /// </summary>
        public static class FileExtensions
        {
            /// <summary>
            /// SQL script file extension
            /// </summary>
            public const string Sql = ".sql";

            /// <summary>
            /// C# script file extension
            /// </summary>
            public const string CSharp = ".cs";

            /// <summary>
            /// All supported file extensions
            /// </summary>
            public static readonly string[] All = { Sql, CSharp };
        }

        /// <summary>
        /// SQL-related constants
        /// </summary>
        public static class Sql
        {
            /// <summary>
            /// Characters that indicate potential SQL injection risk
            /// </summary>
            public static readonly char[] InjectionRiskCharacters = { ';', '\'', '"', '-', '/', '*' };

            /// <summary>
            /// SQL comment patterns to check for
            /// </summary>
            public static readonly string[] CommentPatterns = { "--", "/*", "*/" };
        }

        /// <summary>
        /// Logging message constants
        /// </summary>
        public static class LogMessages
        {
            public const string StartingMigrationProcess = "Starting database reactor process...";
            public const string ApplyingUpgrades = "Applying upgrades...";
            public const string ApplyingDowngrades = "Applying downgrades...";
            public const string MigrationProcessCompleted = "Database reactor process completed successfully.";
            public const string StartingDatabaseMigration = "Starting database migration...";
            public const string EnsureDatabaseExists = "Ensuring database exists...";
            public const string NoPendingMigrations = "No pending migrations found.";
            public const string DatabaseMigrationCompleted = "Database migration completed. Success: {0}";
            public const string DowngradeDisabled = "Downgrade scripts are disabled in configuration. No changes will be reverted.";
            public const string StartingDatabaseDowngrade = "Starting database downgrade...";
            public const string NoMigrationForDowngrade = "No migration found for downgrade.";
            public const string DatabaseDowngradeCompleted = "Database downgrade completed. Success: {0}";
        }

        /// <summary>
        /// Error message constants
        /// </summary>
        public static class ErrorMessages
        {
            public const string ConnectionManagerRequired = "ConnectionManager is required";
            public const string ScriptProviderRequired = "ScriptProvider is required";
            public const string ScriptJournalRequired = "MigrationJournal is required";
            public const string ScriptExecutorRequired = "ScriptExecutor is required";
            public const string DatabaseProvisionerRequired = "DatabaseProvisioner is required when EnsureDatabaseExists is true";
            public const string DowngradeResolverRequired = "DowngradeResolver is required when downgrades are enabled";
            public const string UpgradeScriptContentEmpty = "Upgrade script content is empty";
            public const string DowngradeScriptContentEmpty = "Downgrade script content is empty";
            public const string MigrationDoesNotSupportDowngrade = "Migration {0} does not support downgrade.";
            public const string ConfigurationValidationFailed = "Configuration validation failed:\n{0}";
            
            // Execution errors
            public const string MigrationExecutionFailed = "Failed to execute migration script '{0}': {1}";
            public const string DowngradeExecutionFailed = "Failed to execute downgrade script '{0}': {1}";
            public const string DatabaseConnectionFailed = "Failed to connect to database: {0}";
            public const string JournalTableCreationFailed = "Failed to create migration journal table: {0}";
            public const string ScriptDiscoveryFailed = "Failed to discover scripts: {0}";
            
            // Validation errors
            public const string InvalidScriptContent = "Script content is invalid or contains potential security risks";
            public const string UnsupportedDatabaseProvider = "Database provider '{0}' is not supported";
            public const string InvalidConnectionString = "Connection string is invalid or missing required parameters";
        }
    }
}