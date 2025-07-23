using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services;

public interface IVariableManagementService
{
    Task<Dictionary<string, string>> GetVariablesAsync(string? configurationPath = null, CancellationToken cancellationToken = default);
    Task SaveVariablesAsync(Dictionary<string, string> variables, string? configurationPath = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> ManageVariablesInteractivelyAsync(Dictionary<string, string>? initialVariables = null, CancellationToken cancellationToken = default);
    Task AddVariableAsync(string key, string value, string? configurationPath = null, CancellationToken cancellationToken = default);
    Task RemoveVariableAsync(string key, string? configurationPath = null, CancellationToken cancellationToken = default);
    Task<bool> HasVariablesAsync(string? configurationPath = null, CancellationToken cancellationToken = default);
}