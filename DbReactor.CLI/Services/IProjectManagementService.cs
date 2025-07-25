using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services;

public interface IProjectManagementService
{
    Task<IEnumerable<ProjectInfo>> ScanForProjectsAsync(string searchPath, CancellationToken cancellationToken = default);
    Task<ProjectInfo> CreateProjectAsync(string projectName, string parentPath, CliOptions defaultOptions, CancellationToken cancellationToken = default);
    Task<bool> IsValidProjectAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<ProjectInfo?> GetProjectInfoAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<bool> IsInsideExistingProjectAsync(string path, CancellationToken cancellationToken = default);
}