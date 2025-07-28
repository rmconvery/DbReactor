using DbReactor.Core.Abstractions;
using DbReactor.Core.Constants;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Seeding.Strategies
{
    /// <summary>
    /// Strategy that executes seeds only once
    /// </summary>
    public class RunOnceSeedStrategy : IRunOnceSeedStrategy
    {
        public string Name => DbReactorConstants.SeedStrategies.RunOnce;

        public async Task<bool> ShouldExecuteAsync(ISeed seed, ISeedJournal seedJournal, CancellationToken cancellationToken = default)
        {
            return !await seedJournal.HasBeenExecutedAsync(seed, cancellationToken);
        }
    }
}