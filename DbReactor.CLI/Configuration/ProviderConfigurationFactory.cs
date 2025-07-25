using DbReactor.Core.Configuration;
using DbReactor.MSSqlServer.Extensions;

namespace DbReactor.CLI.Configuration;

public class ProviderConfigurationFactory : IProviderConfigurationFactory
{
    public void ConfigureProvider(DbReactorConfiguration config, string provider, string connectionString)
    {
        if (!IsSqlServerProvider(provider))
        {
            throw new ArgumentException($"Unsupported provider: {provider}. Only SQL Server is supported.");
        }

        config.UseSqlServer(connectionString);
    }

    public IEnumerable<string> GetSupportedProviders() => new[] { "sqlserver", "sql", "mssql" };

    private static bool IsSqlServerProvider(string provider) =>
        string.Equals(provider, "sqlserver", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "sql", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "mssql", StringComparison.OrdinalIgnoreCase);
}