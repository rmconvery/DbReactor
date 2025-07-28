using DbReactor.Core.Abstractions;
using DbReactor.Core.Constants;
using DbReactor.Core.Seeding.Strategies;
using System;
using System.IO;

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
                var directoryName = Path.GetDirectoryName(scriptPath);
                if (string.IsNullOrEmpty(directoryName))
                    return null;

                var folderName = Path.GetFileName(directoryName).ToLowerInvariant();

                if (folderName.Contains(DbReactorConstants.SeedFolders.RunAlways))
                    return new RunAlwaysSeedStrategy();

                if (folderName.Contains(DbReactorConstants.SeedFolders.RunIfChanged))
                    return new RunIfChangedSeedStrategy();

                if (folderName.Contains(DbReactorConstants.SeedFolders.RunOnce))
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
    }
}