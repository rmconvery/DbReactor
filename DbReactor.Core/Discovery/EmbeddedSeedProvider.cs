using DbReactor.Core.Abstractions;
using DbReactor.Core.Models;
using DbReactor.Core.Models.Scripts;
using DbReactor.Core.Seeding.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Discovery
{
    /// <summary>
    /// Discovers embedded resource seed scripts with strategy resolved at discovery time.
    /// Passes the full resource name to strategy resolvers so folder-based resolution works correctly.
    /// </summary>
    public class EmbeddedSeedProvider : ISeedScriptProvider
    {
        private readonly Assembly _assembly;
        private readonly string _resourceNamespace;
        private readonly string _scriptSuffix;
        private readonly IEnumerable<ISeedStrategyResolver> _strategyResolvers;
        private readonly ISeedExecutionStrategy _fallbackStrategy;

        /// <summary>
        /// Initializes a new instance of EmbeddedSeedProvider
        /// </summary>
        /// <param name="assembly">Assembly containing embedded seed resources</param>
        /// <param name="resourceNamespace">Namespace prefix to filter resources</param>
        /// <param name="strategyResolvers">Strategy resolvers to apply using full resource name</param>
        /// <param name="scriptSuffix">File extension filter (default: .sql)</param>
        /// <param name="fallbackStrategy">Fallback strategy when no resolver matches</param>
        public EmbeddedSeedProvider(
            Assembly assembly,
            string resourceNamespace,
            IEnumerable<ISeedStrategyResolver> strategyResolvers,
            string scriptSuffix = ".sql",
            ISeedExecutionStrategy fallbackStrategy = null)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _resourceNamespace = resourceNamespace ?? throw new ArgumentNullException(nameof(resourceNamespace));
            _strategyResolvers = strategyResolvers ?? Enumerable.Empty<ISeedStrategyResolver>();
            _scriptSuffix = scriptSuffix ?? ".sql";
            _fallbackStrategy = fallbackStrategy ?? new RunOnceSeedStrategy();
        }

        /// <summary>
        /// Gets seeds from embedded resources with strategy resolved using the full resource name
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of seeds with strategy resolved</returns>
        public Task<IEnumerable<ISeed>> GetSeedsAsync(CancellationToken cancellationToken = default)
        {
            IEnumerable<ISeed> seeds = _assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(_resourceNamespace, StringComparison.Ordinal)
                    && r.EndsWith(_scriptSuffix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r)
                .Select(resourceName =>
                {
                    try
                    {
                        var script = new EmbeddedScript(_assembly, resourceName);
                        if (string.IsNullOrEmpty(script.Script))
                            return null;

                        // Pass full resource name to strategy resolvers for folder-based resolution
                        var strategy = ResolveStrategy(script, resourceName);

                        // Keep resource name as seed name (journal-compatible with existing EmbeddedScriptProvider behavior)
                        return new Seed(resourceName, script, strategy, script.Hash);
                    }
                    catch (Exception)
                    {
                        // Skip unreadable resources (matches EmbeddedScriptProvider behavior)
                        return null;
                    }
                })
                .Where(s => s != null);

            return Task.FromResult(seeds);
        }

        private ISeedExecutionStrategy ResolveStrategy(IScript script, string resourceName)
        {
            foreach (var resolver in _strategyResolvers)
            {
                var strategy = resolver.ResolveStrategy(script, resourceName);
                if (strategy != null)
                    return strategy;
            }

            return _fallbackStrategy;
        }
    }
}
