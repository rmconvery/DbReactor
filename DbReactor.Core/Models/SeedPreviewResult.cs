using DbReactor.Core.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace DbReactor.Core.Models
{
    /// <summary>
    /// Represents the result of a seed preview execution
    /// </summary>
    public class SeedPreviewResult
    {
        /// <summary>
        /// Seed that would be executed
        /// </summary>
        public ISeed Seed { get; set; }

        /// <summary>
        /// Whether this seed would be executed based on strategy
        /// </summary>
        public bool WouldExecute { get; set; }

        /// <summary>
        /// Seed name for display purposes
        /// </summary>
        public string SeedName { get; set; }

        /// <summary>
        /// Strategy being used for this seed
        /// </summary>
        public string Strategy { get; set; }

        /// <summary>
        /// Reason why the seed would or would not execute
        /// </summary>
        public string ExecutionReason { get; set; }

        /// <summary>
        /// Additional metadata about the seed preview
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the complete result of a seed preview execution
    /// </summary>
    public class DbReactorSeedPreviewResult
    {
        /// <summary>
        /// Individual seed preview results
        /// </summary>
        public List<SeedPreviewResult> SeedResults { get; set; } = new List<SeedPreviewResult>();

        /// <summary>
        /// Total number of seeds discovered
        /// </summary>
        public int TotalSeeds => SeedResults.Count;

        /// <summary>
        /// Number of seeds that would be skipped
        /// </summary>
        public int SkippedSeeds => SeedResults.Count(r => !r.WouldExecute);

        /// <summary>
        /// Number of seeds that would actually be executed
        /// </summary>
        public int PendingSeeds => SeedResults.Count(r => r.WouldExecute);

        /// <summary>
        /// Summary of what would happen
        /// </summary>
        public string Summary => $"Would execute {PendingSeeds} seeds ({SkippedSeeds} would be skipped)";
    }
}