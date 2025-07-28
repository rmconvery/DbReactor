using DbReactor.Core.Abstractions;
using DbReactor.Core.Constants;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Seeding.Strategies
{
    /// <summary>
    /// Strategy that executes seeds only if content has changed
    /// </summary>
    public class RunIfChangedSeedStrategy : IRunIfChangedSeedStrategy
    {
        public string Name => DbReactorConstants.SeedStrategies.RunIfChanged;

        public async Task<bool> ShouldExecuteAsync(ISeed seed, ISeedJournal seedJournal, CancellationToken cancellationToken = default)
        {
            var lastExecutedHash = await seedJournal.GetLastExecutedHashAsync(seed.Name, cancellationToken);
            return lastExecutedHash != seed.Hash;
        }
    }
}