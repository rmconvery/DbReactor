using DbReactor.Core.Abstractions;
using DbReactor.Core.Constants;
using DbReactor.Core.Seeding.Strategies;

namespace DbReactor.Core.Seeding.Resolvers
{
    /// <summary>
    /// Resolves seed strategies based on naming conventions in the script name
    /// </summary>
    public class NamingConventionSeedStrategyResolver : ISeedStrategyResolver
    {
        /// <summary>
        /// Resolves strategy based on naming patterns in the script name
        /// </summary>
        /// <param name="script">The script to resolve strategy for</param>
        /// <param name="scriptPath">Optional path information (not used by this resolver)</param>
        /// <returns>The resolved strategy, or null if no naming convention is found</returns>
        public ISeedExecutionStrategy ResolveStrategy(IScript script, string scriptPath = null)
        {
            var lowerScriptName = script.Name.ToLowerInvariant();

            if (lowerScriptName.Contains(DbReactorConstants.SeedNamingConventions.RunAlways) || 
                lowerScriptName.Contains(DbReactorConstants.SeedNamingConventions.RunAlwaysUnderscore))
                return new RunAlwaysSeedStrategy();
            
            if (lowerScriptName.Contains(DbReactorConstants.SeedNamingConventions.RunIfChanged) || 
                lowerScriptName.Contains(DbReactorConstants.SeedNamingConventions.RunIfChangedUnderscore))
                return new RunIfChangedSeedStrategy();

            if (lowerScriptName.Contains(DbReactorConstants.SeedNamingConventions.RunOnce) || 
                lowerScriptName.Contains(DbReactorConstants.SeedNamingConventions.RunOnceUnderscore))
                return new RunOnceSeedStrategy();

            // No naming convention found
            return null;
        }
    }
}