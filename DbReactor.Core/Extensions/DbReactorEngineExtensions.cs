using DbReactor.Core.Abstractions;
using DbReactor.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Extensions
{
    /// <summary>
    /// Extension methods for IDbReactorEngine to provide synchronous wrappers
    /// </summary>
    public static class DbReactorEngineExtensions
    {
        /// <summary>
        /// Synchronously runs the database migration process
        /// </summary>
        /// <param name="engine">The DbReactor engine instance</param>
        /// <returns>The migration result</returns>
        public static DbReactorResult Run(this IDbReactorEngine engine)
        {
            return Task.Run(async () => await engine.RunAsync(CancellationToken.None).ConfigureAwait(false)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronously applies upgrade migrations
        /// </summary>
        /// <param name="engine">The DbReactor engine instance</param>
        /// <returns>The migration result</returns>
        public static DbReactorResult ApplyUpgrades(this IDbReactorEngine engine)
        {
            return Task.Run(async () => await engine.ApplyUpgradesAsync(CancellationToken.None).ConfigureAwait(false)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronously applies downgrade migrations
        /// </summary>
        /// <param name="engine">The DbReactor engine instance</param>
        /// <returns>The migration result</returns>
        public static DbReactorResult ApplyDowngrades(this IDbReactorEngine engine)
        {
            return Task.Run(async () => await engine.ApplyDowngradesAsync(CancellationToken.None).ConfigureAwait(false)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronously checks if there are pending upgrades
        /// </summary>
        /// <param name="engine">The DbReactor engine instance</param>
        /// <returns>True if there are pending upgrades, false otherwise</returns>
        public static bool HasPendingUpgrades(this IDbReactorEngine engine)
        {
            return Task.Run(async () => await engine.HasPendingUpgradesAsync(CancellationToken.None).ConfigureAwait(false)).GetAwaiter().GetResult();
        }
    }
}