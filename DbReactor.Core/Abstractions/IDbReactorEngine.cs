using DbReactor.Core.Models;
using System.Collections.Generic;

namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Core database reactor engine
    /// </summary>
    public interface IDbReactorEngine
    {
        DbReactorResult Run();
        DbReactorResult ApplyUpgrades();
        DbReactorResult ApplyDowngrades();
        bool HasPendingUpgrades();
        IEnumerable<IMigration> GetPendingUpgrades();
        IEnumerable<IMigration> GetAppliedUpgrades();
    }
}