using System;

namespace DbReactor.Core.Models
{
    /// <summary>
    /// Represents a seed execution journal entry
    /// </summary>
    public class SeedJournalEntry
    {
        /// <summary>
        /// Name of the executed seed
        /// </summary>
        public string SeedName { get; set; }

        /// <summary>
        /// Hash of the seed content when executed
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Strategy used when executed
        /// </summary>
        public string Strategy { get; set; }

        /// <summary>
        /// When the seed was executed
        /// </summary>
        public DateTime ExecutedOn { get; set; }

        /// <summary>
        /// Duration of the seed execution
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}