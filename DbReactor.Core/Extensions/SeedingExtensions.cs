using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Discovery;
using DbReactor.Core.Seeding.Resolvers;
using DbReactor.Core.Seeding.Strategies;
using DbReactor.Core.Services;
using System;

namespace DbReactor.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring seeding functionality
    /// </summary>
    public static class SeedingExtensions
    {
        #region Basic Seeding Configuration

        /// <summary>
        /// Enables seeding functionality with basic configuration
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="seedJournal">The seed journal implementation</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration EnableSeeding(this DbReactorConfiguration config, ISeedJournal seedJournal = null)
        {
            config.EnableSeeding = true;
            if (seedJournal != null)
            {
                config.SeedJournal = seedJournal;
            }
            return config;
        }

        /// <summary>
        /// Disables seeding functionality
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration DisableSeeding(this DbReactorConfiguration config)
        {
            config.EnableSeeding = false;
            return config;
        }

        #endregion

        #region Seed Journal Configuration

        /// <summary>
        /// Sets the seed journal implementation for tracking seed execution
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="seedJournal">The seed journal implementation</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseSeedJournal(this DbReactorConfiguration config, ISeedJournal seedJournal)
        {
            config.SeedJournal = seedJournal ?? throw new ArgumentNullException(nameof(seedJournal));
            return config;
        }

        #endregion

        #region Script Provider Configuration

        /// <summary>
        /// Adds a script provider for seed discovery
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="scriptProvider">The script provider to add</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration AddSeedScriptProvider(this DbReactorConfiguration config, IScriptProvider scriptProvider)
        {
            if (scriptProvider == null) throw new ArgumentNullException(nameof(scriptProvider));
            config.SeedScriptProviders.Add(scriptProvider);
            return config;
        }

        /// <summary>
        /// Clears all existing seed script providers
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration ClearSeedScriptProviders(this DbReactorConfiguration config)
        {
            config.SeedScriptProviders.Clear();
            return config;
        }

        #endregion

        #region Strategy Configuration

        /// <summary>
        /// Sets a global seed execution strategy that applies to all seeds
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="strategy">The global strategy to apply</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseGlobalSeedStrategy(this DbReactorConfiguration config, ISeedExecutionStrategy strategy)
        {
            config.GlobalSeedStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            return config;
        }

        /// <summary>
        /// Sets the fallback seed execution strategy for seeds without specific strategies
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="strategy">The fallback strategy to use</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseFallbackSeedStrategy(this DbReactorConfiguration config, ISeedExecutionStrategy strategy)
        {
            config.FallbackSeedStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            return config;
        }

        /// <summary>
        /// Configures all seeds to use RunOnce strategy (execute only once)
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseRunOnceSeedStrategy(this DbReactorConfiguration config)
        {
            return config.UseGlobalSeedStrategy(new RunOnceSeedStrategy());
        }

        /// <summary>
        /// Configures all seeds to use RunAlways strategy (execute every time)
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseRunAlwaysSeedStrategy(this DbReactorConfiguration config)
        {
            return config.UseGlobalSeedStrategy(new RunAlwaysSeedStrategy());
        }

        /// <summary>
        /// Configures all seeds to use RunIfChanged strategy (execute only when content changes)
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseRunIfChangedSeedStrategy(this DbReactorConfiguration config)
        {
            return config.UseGlobalSeedStrategy(new RunIfChangedSeedStrategy());
        }

        #endregion

        #region Strategy Resolver Configuration

        /// <summary>
        /// Adds a strategy resolver for determining seed execution strategies
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="resolver">The strategy resolver to add</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration AddSeedStrategyResolver(this DbReactorConfiguration config, ISeedStrategyResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            config.SeedStrategyResolvers.Add(resolver);
            return config;
        }

        /// <summary>
        /// Adds folder structure-based strategy resolution (e.g., run-always/, run-once/, run-if-changed/)
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseFolderBasedSeedStrategies(this DbReactorConfiguration config)
        {
            return config.AddSeedStrategyResolver(new FolderStructureSeedStrategyResolver());
        }

        /// <summary>
        /// Adds naming convention-based strategy resolution (e.g., script_runonce.sql, script_runalways.sql)
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseNamingConventionSeedStrategies(this DbReactorConfiguration config)
        {
            return config.AddSeedStrategyResolver(new NamingConventionSeedStrategyResolver());
        }

        /// <summary>
        /// Adds both folder-based and naming convention-based strategy resolution
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseStandardSeedStrategies(this DbReactorConfiguration config)
        {
            return config
                .UseFolderBasedSeedStrategies()
                .UseNamingConventionSeedStrategies();
        }

        /// <summary>
        /// Clears all existing seed strategy resolvers
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration ClearSeedStrategyResolvers(this DbReactorConfiguration config)
        {
            config.SeedStrategyResolvers.Clear();
            return config;
        }

        #endregion

        #region Seed Script Discovery

        /// <summary>
        /// Adds embedded seed scripts from a specific folder
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="assembly">Assembly containing embedded seed scripts</param>
        /// <param name="seedFolder">Folder name containing seed scripts (default: Seeds)</param>
        /// <param name="fileExtension">File extension for seed scripts (default: .sql)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseEmbeddedSeeds(this DbReactorConfiguration config, System.Reflection.Assembly assembly, string seedFolder = "Seeds", string fileExtension = ".sql")
        {
            var baseNamespace = assembly.GetName().Name;
            var seedProvider = new EmbeddedScriptProvider(assembly, $"{baseNamespace}.{seedFolder}", fileExtension);
            return config.AddSeedScriptProvider(seedProvider);
        }

        /// <summary>
        /// Adds embedded seed scripts with explicit namespace
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="assembly">Assembly containing embedded seed scripts</param>
        /// <param name="resourceNamespace">Full namespace where seed scripts are embedded</param>
        /// <param name="fileExtension">File extension for seed scripts (default: .sql)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseEmbeddedSeedsFromNamespace(this DbReactorConfiguration config, System.Reflection.Assembly assembly, string resourceNamespace, string fileExtension = ".sql")
        {
            var seedProvider = new EmbeddedScriptProvider(assembly, resourceNamespace, fileExtension);
            return config.AddSeedScriptProvider(seedProvider);
        }

        /// <summary>
        /// Adds file system-based seed scripts from a directory
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="directoryPath">Directory path containing seed script files</param>
        /// <param name="fileExtension">File extension to search for (default: .sql)</param>
        /// <param name="recursive">Whether to search subdirectories recursively (default: true)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseFileSystemSeeds(this DbReactorConfiguration config, string directoryPath, string fileExtension = ".sql", bool recursive = true)
        {
            var seedProvider = new FileSystemScriptProvider(directoryPath, fileExtension, recursive);
            return config.AddSeedScriptProvider(seedProvider);
        }

        #endregion

        #region Combined Configuration

        /// <summary>
        /// Configures seeding with standard defaults: folder-based strategies, RunOnce fallback
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="seedJournal">The seed journal implementation</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseStandardSeeding(this DbReactorConfiguration config, ISeedJournal seedJournal)
        {
            return config
                .EnableSeeding(seedJournal)
                .UseStandardSeedStrategies()
                .UseFallbackSeedStrategy(new RunOnceSeedStrategy());
        }

        #endregion
    }
}