namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Strategy for seeds that should run only if the content has changed
    /// </summary>
    public interface IRunIfChangedSeedStrategy : ISeedExecutionStrategy
    {
    }
}