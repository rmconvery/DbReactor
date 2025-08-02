using DbReactor.Core.Abstractions;
using DbReactor.Core.Discovery;
using DbReactor.Core.Models;
using DbReactor.Core.Seeding.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Services
{
    /// <summary>
    /// Service for discovering seeds using existing script providers with strategy resolution
    /// </summary>
    public class SeedDiscoveryService
    {
        private readonly IEnumerable<IScriptProvider> _scriptProviders;
        private readonly IEnumerable<ISeedStrategyResolver> _strategyResolvers;
        private readonly ISeedExecutionStrategy _globalStrategy;
        private readonly ISeedExecutionStrategy _fallbackStrategy;

        /// <summary>
        /// Initializes a new instance of SeedDiscoveryService
        /// </summary>
        /// <param name="scriptProviders">Script providers to use for seed discovery</param>
        /// <param name="strategyResolvers">Strategy resolvers for per-seed strategy determination</param>
        /// <param name="globalStrategy">Global strategy to apply to all seeds (overrides per-seed strategies)</param>
        /// <param name="fallbackStrategy">Fallback strategy when no other strategy can be determined</param>
        public SeedDiscoveryService(
            IEnumerable<IScriptProvider> scriptProviders, 
            IEnumerable<ISeedStrategyResolver> strategyResolvers = null,
            ISeedExecutionStrategy globalStrategy = null,
            ISeedExecutionStrategy fallbackStrategy = null)
        {
            _scriptProviders = scriptProviders ?? throw new ArgumentNullException(nameof(scriptProviders));
            _strategyResolvers = strategyResolvers ?? Enumerable.Empty<ISeedStrategyResolver>();
            _globalStrategy = globalStrategy;
            _fallbackStrategy = fallbackStrategy ?? new RunOnceSeedStrategy();
        }

        /// <summary>
        /// Gets all available seeds from configured script providers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of seeds</returns>
        public async Task<IEnumerable<ISeed>> GetSeedsAsync(CancellationToken cancellationToken = default)
        {
            var allScripts = new List<IScript>();
            
            foreach (var provider in _scriptProviders)
            {
                var scripts = await provider.GetScriptsAsync(cancellationToken);
                allScripts.AddRange(scripts);
            }

            return allScripts.Select(script => new Seed(
                script.Name,
                script,
                DetermineStrategy(script, script.Name),
                script.Hash
            ));
        }

        /// <summary>
        /// Determines the appropriate strategy for a script
        /// </summary>
        /// <param name="script">The script to determine strategy for</param>
        /// <param name="scriptPath">Optional path for folder-based resolution</param>
        /// <returns>The determined strategy</returns>
        private ISeedExecutionStrategy DetermineStrategy(IScript script, string scriptPath = null)
        {
            // Global strategy overrides everything
            if (_globalStrategy != null)
                return _globalStrategy;

            // Try each strategy resolver in order
            foreach (var resolver in _strategyResolvers)
            {
                var strategy = resolver.ResolveStrategy(script, scriptPath);
                if (strategy != null)
                    return strategy;
            }

            // Use fallback strategy
            return _fallbackStrategy;
        }
    }
}