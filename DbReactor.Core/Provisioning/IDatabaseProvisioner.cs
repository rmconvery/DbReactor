namespace DbReactor.Core.Provisioning
{
    /// <summary>
    /// Provides database creation and existence checking capabilities
    /// </summary>
    public interface IDatabaseProvisioner
    {
        /// <summary>
        /// Checks if the target database exists
        /// </summary>
        /// <returns>True if database exists, false otherwise</returns>
        bool DatabaseExists();

        /// <summary>
        /// Creates the target database
        /// </summary>
        /// <param name="template">Optional SQL template for database creation. Use {0} as placeholder for database name.</param>
        void CreateDatabase(string template = null);

        /// <summary>
        /// Ensures the target database exists, creating it if necessary
        /// </summary>
        /// <param name="template">Optional SQL template for database creation. Use {0} as placeholder for database name.</param>
        void EnsureDatabaseExists(string template = null);
    }
}