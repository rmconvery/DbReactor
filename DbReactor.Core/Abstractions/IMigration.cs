namespace DbReactor.Core.Abstractions
{
    public interface IMigration
    {
        string Name { get; }
        IScript UpgradeScript { get; }
        IScript DowngradeScript { get; }
        bool HasDowngrade { get; }
    }
}
