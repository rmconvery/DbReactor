namespace DbReactor.Core.Abstractions
{
    /// <summary>
    /// Strategy for seeds that should run only once
    /// </summary>
    public interface IRunOnceSeedStrategy : ISeedExecutionStrategy
    {
    }
}