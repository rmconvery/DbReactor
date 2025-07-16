using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Extensions
{
    /// <summary>
    /// Extension methods for dry run functionality
    /// </summary>
    public static class DryRunExtensions
    {
        /// <summary>
        /// Enables dry run mode for the configuration
        /// </summary>
        /// <param name="configuration">DbReactor configuration</param>
        /// <returns>Updated configuration</returns>
        public static DbReactorConfiguration EnableDryRun(this DbReactorConfiguration configuration)
        {
            configuration.DryRun = true;
            return configuration;
        }

        /// <summary>
        /// Disables dry run mode for the configuration
        /// </summary>
        /// <param name="configuration">DbReactor configuration</param>
        /// <returns>Updated configuration</returns>
        public static DbReactorConfiguration DisableDryRun(this DbReactorConfiguration configuration)
        {
            configuration.DryRun = false;
            return configuration;
        }

        /// <summary>
        /// Runs a preview of migrations without executing them
        /// </summary>
        /// <param name="engine">DbReactor engine</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Preview result</returns>
        public static async Task<DbReactorDryRunResult> PreviewAsync(this DbReactorEngine engine, CancellationToken cancellationToken = default)
        {
            return await engine.DryRunUpgradesAsync(cancellationToken);
        }

        /// <summary>
        /// Runs a preview of downgrade migrations without executing them
        /// </summary>
        /// <param name="engine">DbReactor engine</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Preview result</returns>
        public static async Task<DbReactorDryRunResult> PreviewDowngradesAsync(this DbReactorEngine engine, CancellationToken cancellationToken = default)
        {
            return await engine.DryRunDowngradesAsync(cancellationToken);
        }
    }
}