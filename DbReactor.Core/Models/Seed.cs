using DbReactor.Core.Abstractions;

namespace DbReactor.Core.Models
{
    /// <summary>
    /// Concrete implementation of ISeed
    /// </summary>
    public class Seed : ISeed
    {
        public string Name { get; }
        public IScript Script { get; }
        public ISeedExecutionStrategy Strategy { get; }
        public string Hash { get; }

        public Seed(string name, IScript script, ISeedExecutionStrategy strategy, string hash)
        {
            Name = name;
            Script = script;
            Strategy = strategy;
            Hash = hash;
        }
    }
}