namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Represents a database script that can be executed for both upgrades and downgrades
    /// </summary>
    public interface ICodeGeneratedScript
    {
        bool HasDowngrade { get; }

        /// <summary>
        /// Gets the upgrade script content
        /// </summary>
        /// <returns>The SQL script for upgrading the database</returns>
        string GetUpgradeScript();

        /// <summary>
        /// Gets the downgrade script content if available
        /// </summary>
        /// <returns>The SQL script for downgrading the database, or null if not supported</returns>
        string GetDowngradeScript();
    }
}
