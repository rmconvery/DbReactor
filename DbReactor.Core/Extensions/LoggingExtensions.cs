using DbReactor.Core.Configuration;
using DbReactor.Core.Logging;

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
    }
}