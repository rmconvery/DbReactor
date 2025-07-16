using DbReactor.Core.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace DbReactor.Core.Models
{
    /// <summary>
    /// Represents the result of a run preview migration execution
    /// </summary>
    public class RunPreviewResult
    {
        /// <summary>
        /// Migration that would be executed
        /// </summary>
        public IMigration Migration { get; set; }

        /// <summary>
        /// Whether this migration has already been executed
        /// </summary>
        public bool AlreadyExecuted { get; set; }

        /// <summary>
        /// Migration name for display purposes
        /// </summary>
        public string MigrationName { get; set; }

        /// <summary>
        /// Whether this is an upgrade or downgrade script
        /// </summary>
        public bool IsUpgrade { get; set; } = true;

        /// <summary>
        /// Additional metadata about the run preview
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the complete result of a run preview execution
    /// </summary>
    public class DbReactorPreviewResult
    {
        /// <summary>
        /// Individual migration run preview results
        /// </summary>
        public List<RunPreviewResult> MigrationResults { get; set; } = new List<RunPreviewResult>();

        /// <summary>
        /// Total number of migrations discovered
        /// </summary>
        public int TotalMigrations => MigrationResults.Count;

        /// <summary>
        /// Number of migrations that would be skipped (already executed)
        /// </summary>
        public int SkippedMigrations => MigrationResults.Count(r => r.AlreadyExecuted);

        /// <summary>
        /// Number of migrations that would actually be executed
        /// </summary>
        public int PendingMigrations => MigrationResults.Count(r => !r.AlreadyExecuted);

        /// <summary>
        /// Number of upgrade migrations that would be executed
        /// </summary>
        public int PendingUpgrades => MigrationResults.Count(r => !r.AlreadyExecuted && r.IsUpgrade);

        /// <summary>
        /// Number of downgrade migrations that would be executed
        /// </summary>
        public int PendingDowngrades => MigrationResults.Count(r => !r.AlreadyExecuted && !r.IsUpgrade);

        /// <summary>
        /// Number of upgrade migrations already executed
        /// </summary>
        public int SkippedUpgrades => MigrationResults.Count(r => r.AlreadyExecuted && r.IsUpgrade);

        /// <summary>
        /// Number of downgrade migrations already executed
        /// </summary>
        public int SkippedDowngrades => MigrationResults.Count(r => r.AlreadyExecuted && !r.IsUpgrade);

        /// <summary>
        /// Summary of what would happen
        /// </summary>
        public string Summary => $"Would execute {PendingMigrations} migrations ({SkippedMigrations} already executed)";
    }
}