using System;

namespace DbReactor.MSSqlServer.Constants
{
    /// <summary>
    /// Constants specific to SQL Server implementation
    /// </summary>
    public static class SqlServerConstants
    {
        /// <summary>
        /// Default values for SQL Server configuration
        /// </summary>
        public static class Defaults
        {
            /// <summary>
            /// Default schema name for SQL Server
            /// </summary>
            public const string SchemaName = "dbo";

            /// <summary>
            /// Default journal table name for SQL Server
            /// </summary>
            public const string JournalTableName = "__migration_journal";

            /// <summary>
            /// Default command timeout
            /// </summary>
            public static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);
        }
    }
}