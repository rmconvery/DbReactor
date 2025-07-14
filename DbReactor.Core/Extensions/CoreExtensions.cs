using DbReactor.Core.Configuration;
using DbReactor.Core.Discovery;
using System;

namespace DbReactor.Core.Extensions
{
    /// <summary>
    /// Legacy extension methods for backward compatibility
    /// Use the specific extension classes (LoggingExtensions, ScriptDiscoveryExtensions, etc.) for new code
    /// </summary>
    [Obsolete("Use specific extension classes like LoggingExtensions, ScriptDiscoveryExtensions, MigrationBehaviorExtensions, and DatabaseManagementExtensions instead")]
    public static class CoreExtensions
    {
        /// <summary>
        /// Adds a script provider to the configuration
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="provider">Script provider to add</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration AddScriptProvider(this DbReactorConfiguration config, IScriptProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            config.ScriptProviders.Add(provider);
            return config;
        }
    }
}