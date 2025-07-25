using DbReactor.Core.Configuration;
using DbReactor.Core.Logging;
using System;

namespace DbReactor.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring logging in DbReactor
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Enables console logging for migration execution
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration UseConsoleLogging(this DbReactorConfiguration config)
        {
            config.LogProvider = new ConsoleLogProvider();
            return config;
        }

        /// <summary>
        /// Sets a custom log provider for migration execution
        /// </summary>
        /// <param name="config">The configuration to extend</param>
        /// <param name="logProvider">Custom log provider to use</param>
        /// <returns>The configuration for method chaining</returns>
        public static DbReactorConfiguration AddLogProvider(this DbReactorConfiguration config, ILogProvider logProvider)
        {
            if (logProvider == null) throw new ArgumentNullException(nameof(logProvider));

            config.LogProvider = logProvider;
            return config;
        }
    }
}