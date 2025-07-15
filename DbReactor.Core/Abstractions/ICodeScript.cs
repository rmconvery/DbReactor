using DbReactor.Core.Models.Contexts;

namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Interface for code-based migration scripts that generate SQL for upgrade and optional downgrade operations
    /// </summary>
    public interface ICodeScript
    {
        /// <summary>
        /// Generates the SQL for the upgrade operation
        /// </summary>
        /// <param name="context">Context containing database connection and variables</param>
        /// <returns>SQL script to execute for upgrade</returns>
        string GetUpgradeScript(CodeScriptContext context);

        /// <summary>
        /// Generates the SQL for the downgrade operation (optional)
        /// </summary>
        /// <param name="context">Context containing database connection and variables</param>
        /// <returns>SQL script to execute for downgrade, or null if not supported</returns>
        string GetDowngradeScript(CodeScriptContext context);

        /// <summary>
        /// Indicates whether this script supports downgrade operations
        /// </summary>
        bool SupportsDowngrade { get; }
    }
}