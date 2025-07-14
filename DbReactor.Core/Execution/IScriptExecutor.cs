using DbReactor.Core.Abstractions;
using DbReactor.Core.Models;

namespace DbReactor.Core.Execution
{
    /// <summary>
    /// Executes database scripts
    /// </summary>
    public interface IScriptExecutor
    {
        MigrationResult Execute(IScript script, IConnectionManager connectionManager);
        void VerifySchema(IConnectionManager connectionManager);
    }
}
