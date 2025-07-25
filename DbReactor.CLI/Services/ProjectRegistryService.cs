using System.Text.Json;
using DbReactor.CLI.Models;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Services;

public class ProjectRegistryService : IProjectRegistryService
{
    private readonly ILogger<ProjectRegistryService> _logger;
    private readonly string _registryFilePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ProjectRegistryService(ILogger<ProjectRegistryService> logger)
    {
        _logger = logger;
        var userConfigDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbReactorDir = Path.Combine(userConfigDir, "DbReactor");
        Directory.CreateDirectory(dbReactorDir);
        _registryFilePath = Path.Combine(dbReactorDir, "projects.json");
    }

    public async Task<ProjectRegistry> LoadRegistryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_registryFilePath))
            {
                _logger.LogDebug("Registry file not found, creating new registry");
                return new ProjectRegistry();
            }

            var json = await File.ReadAllTextAsync(_registryFilePath, cancellationToken);
            var registry = JsonSerializer.Deserialize<ProjectRegistry>(json, JsonOptions);
            
            _logger.LogDebug("Loaded registry with {ProjectCount} projects", registry?.Projects.Count ?? 0);
            return registry ?? new ProjectRegistry();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load project registry, creating new registry");
            return new ProjectRegistry();
        }
    }

    public async Task SaveRegistryAsync(ProjectRegistry registry, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(registry, JsonOptions);
            await File.WriteAllTextAsync(_registryFilePath, json, cancellationToken);
            _logger.LogDebug("Saved registry with {ProjectCount} projects", registry.Projects.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project registry");
            throw;
        }
    }

    public async Task RegisterProjectAsync(string name, string path, string? description = null, CancellationToken cancellationToken = default)
    {
        var registry = await LoadRegistryAsync(cancellationToken);
        
        var absolutePath = Path.GetFullPath(path);
        var registeredProject = new RegisteredProject
        {
            Name = name,
            Path = absolutePath,
            Description = description,
            LastAccessed = DateTime.Now,
            IsValid = Directory.Exists(absolutePath)
        };

        registry.Projects[name] = registeredProject;
        await SaveRegistryAsync(registry, cancellationToken);
        
        _logger.LogInformation("Registered project '{ProjectName}' at '{ProjectPath}'", name, absolutePath);
    }

    public async Task UnregisterProjectAsync(string name, CancellationToken cancellationToken = default)
    {
        var registry = await LoadRegistryAsync(cancellationToken);
        
        if (registry.Projects.Remove(name))
        {
            await SaveRegistryAsync(registry, cancellationToken);
            _logger.LogInformation("Unregistered project '{ProjectName}'", name);
        }
        else
        {
            _logger.LogWarning("Project '{ProjectName}' not found in registry", name);
        }
    }

    public async Task AddWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        var registry = await LoadRegistryAsync(cancellationToken);
        var absolutePath = Path.GetFullPath(workspacePath);
        
        if (!registry.WorkspaceDirectories.Contains(absolutePath))
        {
            registry.WorkspaceDirectories.Add(absolutePath);
            await SaveRegistryAsync(registry, cancellationToken);
            _logger.LogInformation("Added workspace directory: {WorkspacePath}", absolutePath);
        }
    }

    public async Task RemoveWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        var registry = await LoadRegistryAsync(cancellationToken);
        var absolutePath = Path.GetFullPath(workspacePath);
        
        if (registry.WorkspaceDirectories.Remove(absolutePath))
        {
            await SaveRegistryAsync(registry, cancellationToken);
            _logger.LogInformation("Removed workspace directory: {WorkspacePath}", absolutePath);
        }
    }

    public async Task<IEnumerable<RegisteredProject>> GetRegisteredProjectsAsync(CancellationToken cancellationToken = default)
    {
        var registry = await LoadRegistryAsync(cancellationToken);
        return registry.Projects.Values.OrderByDescending(p => p.LastAccessed);
    }

    public async Task<RegisteredProject?> FindProjectByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var registry = await LoadRegistryAsync(cancellationToken);
        return registry.Projects.TryGetValue(name, out var project) ? project : null;
    }

    public async Task UpdateProjectAccessTimeAsync(string name, CancellationToken cancellationToken = default)
    {
        var registry = await LoadRegistryAsync(cancellationToken);
        
        if (registry.Projects.TryGetValue(name, out var project))
        {
            project.LastAccessed = DateTime.Now;
            await SaveRegistryAsync(registry, cancellationToken);
        }
    }

    public async Task ValidateAndCleanupRegistryAsync(CancellationToken cancellationToken = default)
    {
        var registry = await LoadRegistryAsync(cancellationToken);
        var hasChanges = false;

        // Validate project paths
        foreach (var project in registry.Projects.Values.ToList())
        {
            var isValid = Directory.Exists(project.Path);
            if (project.IsValid != isValid)
            {
                project.IsValid = isValid;
                hasChanges = true;
                _logger.LogDebug("Updated validation status for project '{ProjectName}': {IsValid}", 
                    project.Name, isValid);
            }
        }

        // Validate workspace directories
        for (int i = registry.WorkspaceDirectories.Count - 1; i >= 0; i--)
        {
            if (!Directory.Exists(registry.WorkspaceDirectories[i]))
            {
                _logger.LogDebug("Removing invalid workspace directory: {WorkspacePath}", 
                    registry.WorkspaceDirectories[i]);
                registry.WorkspaceDirectories.RemoveAt(i);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await SaveRegistryAsync(registry, cancellationToken);
        }
    }
}