using DbReactor.CLI.Models;
using DbReactor.Core.Configuration;

namespace DbReactor.CLI.Configuration;

public interface ICliConfigurationService
{
    Task<DbReactorConfiguration> BuildConfigurationAsync(CliOptions options, CancellationToken cancellationToken = default);
    CliOptions GetDefaultOptions();
    Task<bool> SaveConfigurationAsync(string configPath, CliOptions options, CancellationToken cancellationToken = default);
    Task<CliOptions> LoadConfigurationAsync(string? configPath = null, CancellationToken cancellationToken = default);
}