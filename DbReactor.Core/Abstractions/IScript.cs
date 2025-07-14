namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Represents a database script that can be executed for both upgrades and downgrades
    /// </summary>
    public interface IScript
    {
        string Name { get; }
        string Script { get; }
        string Hash { get; }
    }
}