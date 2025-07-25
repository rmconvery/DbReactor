using DbReactor.Core.Configuration;

namespace DbReactor.CLI.Configuration;

public interface IProviderConfigurationFactory
{
    void ConfigureProvider(DbReactorConfiguration config, string provider, string connectionString);
    IEnumerable<string> GetSupportedProviders();
}