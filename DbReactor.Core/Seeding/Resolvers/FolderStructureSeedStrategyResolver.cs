using DbReactor.Core.Abstractions;
using DbReactor.Core.Constants;
using DbReactor.Core.Seeding.Strategies;
using System;
using System.IO;
using System.Linq;

namespace DbReactor.Core.Seeding.Resolvers
{
    /// <summary>
    /// Resolves seed strategies based on folder structure conventions
    /// </summary>
    public class FolderStructureSeedStrategyResolver : ISeedStrategyResolver
    {
        /// <summary>
        /// Resolves strategy based on folder structure patterns
        /// </summary>
        /// <param name="script">The script to resolve strategy for</param>
        /// <param name="scriptPath">Path information used to determine folder structure</param>
        /// <returns>The resolved strategy, or null if no folder convention is found</returns>
        public ISeedExecutionStrategy ResolveStrategy(IScript script, string scriptPath = null)
        {
            if (string.IsNullOrEmpty(scriptPath))
                return null;

            try
            {
                string pathToAnalyze;
                
                // Handle embedded resource names (contain dots) vs file system paths
                if (scriptPath.Contains(".") && !scriptPath.Contains("\\") && !scriptPath.Contains("/"))
                {
                    // This appears to be an embedded resource name like "DbReactor.RunTest.Seeds.run_always.S003_UpdateStatistics.sql"
                    // Remove the file extension first, then convert dots to path separators for analysis
                    var resourceNamespace = scriptPath.Substring(0, scriptPath.LastIndexOf('.'));
                    pathToAnalyze = resourceNamespace.Replace('.', Path.DirectorySeparatorChar);
                }
                else
                {
                    pathToAnalyze = scriptPath;
                }

                var directoryName = Path.GetDirectoryName(pathToAnalyze);
                if (string.IsNullOrEmpty(directoryName))
                    return null;

                var folderName = Path.GetFileName(directoryName).ToLowerInvariant();

                // Normalize folder name by replacing common separators with a standard one
                var normalizedFolderName = folderName.Replace('_', '-').Replace(' ', '-');

                if (IsStrategyMatch(normalizedFolderName, "run", "always"))
                    return new RunAlwaysSeedStrategy();

                if (IsStrategyMatch(normalizedFolderName, "run", "if", "changed"))
                    return new RunIfChangedSeedStrategy();

                if (IsStrategyMatch(normalizedFolderName, "run", "once"))
                    return new RunOnceSeedStrategy();

                // No folder convention found
                return null;
            }
            catch (Exception)
            {
                // If path parsing fails, return null
                return null;
            }
        }

        /// <summary>
        /// Checks if a folder name matches a strategy pattern using flexible matching
        /// </summary>
        /// <param name="folderName">The normalized folder name to check</param>
        /// <param name="parts">The parts of the strategy name to match</param>
        /// <returns>True if the folder name contains all the strategy parts</returns>
        private static bool IsStrategyMatch(string folderName, params string[] parts)
        {
            if (string.IsNullOrEmpty(folderName) || parts == null || parts.Length == 0)
                return false;

            // Check if all parts are present in the folder name
            return parts.All(part => folderName.Contains(part));
        }
    }
}