using System;

namespace DbReactor.Core.Models
{
    /// <summary>
    /// Represents a journal entry for reporting
    /// </summary>
    public class MigrationJournalEntry
    {
        public int Id { get; set; }
        public string UpgradeScriptHash { get; set; }
        public string MigrationName { get; set; }
        public string DowngradeScript { get; set; }
        public DateTime MigratedOn { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }
}