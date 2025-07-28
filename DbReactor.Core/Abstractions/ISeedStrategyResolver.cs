namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Resolves the appropriate execution strategy for a seed script
    /// </summary>
    public interface ISeedStrategyResolver
    {
        /// <summary>
        /// Attempts to resolve an execution strategy for the given script
        /// </summary>
        /// <param name="script">The script to resolve strategy for</param>
        /// <param name="scriptPath">Optional path information for folder-based resolution</param>
        /// <returns>The resolved execution strategy, or null if this resolver cannot determine the strategy</returns>
        ISeedExecutionStrategy ResolveStrategy(IScript script, string scriptPath = null);
    }
}