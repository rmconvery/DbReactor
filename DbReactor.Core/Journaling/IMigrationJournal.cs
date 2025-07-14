using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using System.Collections.Generic;

namespace DbReactor.Core.Journaling
{
    /// <summary>
    /// Manages the database journal/migrations of executed scripts
    /// </summary>
    public interface IMigrationJournal
    {
        IEnumerable<MigrationJournalEntry> GetExecutedMigrations();
        void StoreExecutedMigration(IMigration migration, MigrationResult result);
        void RemoveExecutedMigration(string upgradeScriptHash);
        bool HasBeenExecuted(IMigration migration);
        void EnsureTableExists(IConnectionManager connectionManager);
    }
}
