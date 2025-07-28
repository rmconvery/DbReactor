using DbReactor.Core.Abstractions;
using DbReactor.Core.Constants;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Seeding.Strategies
{
    /// <summary>
    /// Strategy that executes seeds every time
    /// </summary>
    public class RunAlwaysSeedStrategy : IRunAlwaysSeedStrategy
    {
        public string Name => DbReactorConstants.SeedStrategies.RunAlways;

        public Task<bool> ShouldExecuteAsync(ISeed seed, ISeedJournal seedJournal, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}