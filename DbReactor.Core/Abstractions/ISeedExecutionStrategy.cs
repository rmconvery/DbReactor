using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Base interface for seed execution strategies
    /// </summary>
    public interface ISeedExecutionStrategy
    {
        /// <summary>
        /// Determines if the seed should be executed
        /// </summary>
        /// <param name="seed">The seed to evaluate</param>
        /// <param name="seedJournal">Journal for checking execution history</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the seed should be executed</returns>
        Task<bool> ShouldExecuteAsync(ISeed seed, ISeedJournal seedJournal, CancellationToken cancellationToken = default);

        /// <summary>
        /// Strategy name for identification
        /// </summary>
        string Name { get; }
    }
}