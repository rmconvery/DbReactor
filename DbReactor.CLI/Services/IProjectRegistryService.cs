using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services;

public interface IProjectRegistryService
{
    Task<ProjectRegistry> LoadRegistryAsync(CancellationToken cancellationToken = default);
    Task SaveRegistryAsync(ProjectRegistry registry, CancellationToken cancellationToken = default);
    Task RegisterProjectAsync(string name, string path, string? description = null, CancellationToken cancellationToken = default);
    Task UnregisterProjectAsync(string name, CancellationToken cancellationToken = default);
    Task AddWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default);
    Task RemoveWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default);
    Task<IEnumerable<RegisteredProject>> GetRegisteredProjectsAsync(CancellationToken cancellationToken = default);
    Task<RegisteredProject?> FindProjectByNameAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateProjectAccessTimeAsync(string name, CancellationToken cancellationToken = default);
    Task ValidateAndCleanupRegistryAsync(CancellationToken cancellationToken = default);
}