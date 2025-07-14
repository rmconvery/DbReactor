using DbReactor.Core.Configuration;

namespace DbReactor.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring database management in DbReactor
    /// </summary>
    public static class DatabaseManagementExtensions
    {
        /// <summary>
        /// Enables automatic database creation if the database doesn't exist
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="creationTemplate">Optional SQL template for database creation. Use {0} as placeholder for database name.</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration CreateDatabaseIfNotExists(this DbReactorConfiguration config, string creationTemplate = null)
        {
            config.CreateDatabaseIfNotExists = true;
            config.DatabaseCreationTemplate = creationTemplate;
            return config;
        }
    }
}