using DbReactor.Core.Configuration;
using DbReactor.Core.Discovery;
using DbReactor.Core.Utilities;
using System.Reflection;

namespace DbReactor.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring script discovery in DbReactor
    /// </summary>
    public static class ScriptDiscoveryExtensions
    {
        #region Embedded SQL Scripts

        /// <summary>
        /// Discovers SQL scripts embedded as resources in the assembly
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="assembly">Assembly containing embedded script resources</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseEmbeddedScripts(this DbReactorConfiguration config, Assembly assembly)
        {
            config.ScriptProviders.Add(new EmbeddedScriptProvider(assembly));
            ConfigurationUtility.RefreshMigrationBuilder(config);
            return config;
        }

        /// <summary>
        /// Discovers SQL scripts embedded as resources with explicit namespace and file extension
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="assembly">Assembly containing embedded script resources</param>
        /// <param name="resourceNamespace">The namespace where scripts are embedded</param>
        /// <param name="fileExtension">File extension of script files (default: .sql)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseEmbeddedScripts(this DbReactorConfiguration config, Assembly assembly, string resourceNamespace, string fileExtension = ".sql")
        {
            config.ScriptProviders.Add(new EmbeddedScriptProvider(assembly, resourceNamespace, fileExtension));
            ConfigurationUtility.RefreshMigrationBuilder(config);
            return config;
        }

        /// <summary>
        /// Discovers SQL scripts from a specific folder within embedded resources
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="assembly">Assembly containing embedded script resources</param>
        /// <param name="baseNamespace">Base namespace for the embedded resources</param>
        /// <param name="folderName">Folder name containing the scripts</param>
        /// <param name="fileExtension">File extension of script files (default: .sql)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseEmbeddedScriptsFromFolder(this DbReactorConfiguration config, Assembly assembly, string baseNamespace, string folderName, string fileExtension = ".sql")
        {
            config.ScriptProviders.Add(new EmbeddedScriptProvider(assembly, baseNamespace, folderName, fileExtension));
            ConfigurationUtility.RefreshMigrationBuilder(config);
            return config;
        }

        #endregion

        #region Code Scripts

        /// <summary>
        /// Discovers C# code scripts that implement ICodeScript interface
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="assembly">Assembly containing code script classes</param>
        /// <param name="targetNamespace">Optional namespace filter for code scripts</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseCodeScripts(this DbReactorConfiguration config, Assembly assembly, string targetNamespace = null)
        {
            config.ScriptProviders.Add(new AssemblyCodeScriptProvider(assembly, targetNamespace));
            ConfigurationUtility.RefreshMigrationBuilder(config);
            return config;
        }

        #endregion

        #region Downgrade Configuration

        /// <summary>
        /// Enables downgrade operations using scripts from a separate folder
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="assembly">Assembly containing downgrade scripts</param>
        /// <param name="baseNamespace">Base namespace for embedded resources</param>
        /// <param name="downgradeFolder">Folder containing downgrade scripts</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseDowngradesFromFolder(this DbReactorConfiguration config, Assembly assembly, string baseNamespace, string downgradeFolder)
        {
            var options = new DowngradeMatchingOptions
            {
                Mode = DowngradeMatchingMode.SameName,
                UpgradeSuffix = ".sql",
                DowngradeSuffix = ".sql"
            };
            config.DowngradeResolver = new EmbeddedDowngradeResolver(assembly, baseNamespace, downgradeFolder, options);
            config.AllowDowngrades = true;
            ConfigurationUtility.RefreshMigrationBuilder(config);
            return config;
        }

        /// <summary>
        /// Enables downgrade operations using scripts with a naming suffix
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="assembly">Assembly containing downgrade scripts</param>
        /// <param name="baseNamespace">Base namespace for embedded resources</param>
        /// <param name="downgradeFolder">Folder containing downgrade scripts</param>
        /// <param name="suffix">Suffix used to match downgrade scripts (default: _downgrade)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseDowngradesWithSuffix(this DbReactorConfiguration config, Assembly assembly, string baseNamespace, string downgradeFolder, string suffix = "_downgrade")
        {
            var options = new DowngradeMatchingOptions
            {
                Mode = DowngradeMatchingMode.Suffix,
                Pattern = suffix,
                UpgradeSuffix = ".sql",
                DowngradeSuffix = ".sql"
            };
            config.DowngradeResolver = new EmbeddedDowngradeResolver(assembly, baseNamespace, downgradeFolder, options);
            config.AllowDowngrades = true;
            ConfigurationUtility.RefreshMigrationBuilder(config);
            return config;
        }

        /// <summary>
        /// Enables downgrade operations using scripts with a naming prefix
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="assembly">Assembly containing downgrade scripts</param>
        /// <param name="baseNamespace">Base namespace for embedded resources</param>
        /// <param name="downgradeFolder">Folder containing downgrade scripts</param>
        /// <param name="prefix">Prefix used to match downgrade scripts (default: downgrade_)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseDowngradesWithPrefix(this DbReactorConfiguration config, Assembly assembly, string baseNamespace, string downgradeFolder, string prefix = "downgrade_")
        {
            var options = new DowngradeMatchingOptions
            {
                Mode = DowngradeMatchingMode.Prefix,
                Pattern = prefix,
                UpgradeSuffix = ".sql",
                DowngradeSuffix = ".sql"
            };
            config.DowngradeResolver = new EmbeddedDowngradeResolver(assembly, baseNamespace, downgradeFolder, options);
            config.AllowDowngrades = true;
            ConfigurationUtility.RefreshMigrationBuilder(config);
            return config;
        }

        #endregion

        #region Convenient Presets

        /// <summary>
        /// Sets up the standard folder structure: Scripts/upgrades and Scripts/downgrades
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="assembly">Assembly containing embedded scripts</param>
        /// <param name="upgradeFolder">Folder containing upgrade scripts (default: upgrades)</param>
        /// <param name="downgradeFolder">Folder containing downgrade scripts (default: downgrades)</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseStandardFolderStructure(this DbReactorConfiguration config, Assembly assembly, string upgradeFolder = "upgrades", string downgradeFolder = "downgrades")
        {
            string baseNamespace = AssemblyResourceUtility.DiscoverBaseNamespace(assembly);
            string normalizedUpgradeFolder = PathUtility.NormalizeToNamespace(upgradeFolder);
            string normalizedDowngradeFolder = PathUtility.NormalizeToNamespace(downgradeFolder);

            return config
                .UseEmbeddedScriptsFromFolder(assembly, baseNamespace, normalizedUpgradeFolder)
                .UseDowngradesFromFolder(assembly, baseNamespace, normalizedDowngradeFolder);
        }

        #endregion

    }
}