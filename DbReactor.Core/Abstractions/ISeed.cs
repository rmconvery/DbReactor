namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Represents a database seed script
    /// </summary>
    public interface ISeed
    {
        /// <summary>
        /// Name of the seed script
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The script content or implementation
        /// </summary>
        IScript Script { get; }

        /// <summary>
        /// Strategy for when to execute this seed
        /// </summary>
        ISeedExecutionStrategy Strategy { get; }

        /// <summary>
        /// Hash of the seed content for change detection
        /// </summary>
        string Hash { get; }
    }
}