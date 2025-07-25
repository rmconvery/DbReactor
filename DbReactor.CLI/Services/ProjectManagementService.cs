using DbReactor.CLI.Configuration;
using DbReactor.CLI.Models;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Services;

public class ProjectManagementService : IProjectManagementService
{
    private readonly ICliConfigurationService _configurationService;
    private readonly IDirectoryService _directoryService;
    private readonly IProjectRegistryService _projectRegistryService;
    private readonly ILogger<ProjectManagementService> _logger;

    private const string ProjectConfigFileName = "dbreactor.json";

    public ProjectManagementService(
        ICliConfigurationService configurationService,
        IDirectoryService directoryService,
        IProjectRegistryService projectRegistryService,
        ILogger<ProjectManagementService> logger)
    {
        _configurationService = configurationService;
        _directoryService = directoryService;
        _projectRegistryService = projectRegistryService;
        _logger = logger;
    }

    public async Task<IEnumerable<ProjectInfo>> ScanForProjectsAsync(string searchPath, CancellationToken cancellationToken = default)
    {
        var projects = new List<ProjectInfo>();

        try
        {
            // Method 1: Get registered projects from registry
            var registeredProjects = await GetRegisteredProjectsAsync(cancellationToken);
            projects.AddRange(registeredProjects);

            // Method 2: Check current directory context (walk up tree)
            var contextualProject = await FindProjectInCurrentContextAsync(cancellationToken);
            if (contextualProject != null && !projects.Any(p => p.Path == contextualProject.Path))
            {
                projects.Add(contextualProject);
            }

            // Method 3: Scan workspace directories if specified
            var workspaceProjects = await ScanWorkspaceDirectoriesAsync(cancellationToken);
            foreach (var workspaceProject in workspaceProjects)
            {
                if (!projects.Any(p => p.Path == workspaceProject.Path))
                {
                    projects.Add(workspaceProject);
                }
            }

            // Method 4: Scan provided search path as fallback
            if (!string.IsNullOrEmpty(searchPath) && Directory.Exists(searchPath))
            {
                var searchPathProjects = await ScanDirectoryForProjectsAsync(searchPath, cancellationToken);
                foreach (var searchProject in searchPathProjects)
                {
                    if (!projects.Any(p => p.Path == searchProject.Path))
                    {
                        projects.Add(searchProject);
                    }
                }
            }

            _logger.LogDebug("Found {ProjectCount} projects from all sources", projects.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for projects");
        }

        return projects.OrderByDescending(p => p.Name);
    }

    public async Task<ProjectInfo> CreateProjectAsync(string projectName, string parentPath, CliOptions defaultOptions, CancellationToken cancellationToken = default)
    {
        var projectPath = Path.Combine(parentPath, projectName);
        
        await ValidateProjectCreationAsync(projectPath, cancellationToken);

        try
        {
            CreateProjectDirectory(projectPath);
            CreateProjectStructure(projectPath);
            await SaveProjectConfiguration(projectPath, projectName, defaultOptions, cancellationToken);
        await RegisterNewProjectAsync(projectName, projectPath, cancellationToken);

            _logger.LogInformation("Created DbReactor project: {ProjectName} at {ProjectPath}", projectName, projectPath);

            return new ProjectInfo
            {
                Name = projectName,
                Path = projectPath
            };
        }
        catch (Exception ex)
        {
            CleanupFailedProject(projectPath);
            _logger.LogError(ex, "Failed to create project: {ProjectName}", projectName);
            throw;
        }
    }

    public async Task<bool> IsValidProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(projectPath))
        {
            return false;
        }

        return HasMigrationStructure(projectPath);
    }

    public async Task<ProjectInfo?> GetProjectInfoAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        if (!await IsValidProjectAsync(projectPath, cancellationToken))
        {
            return null;
        }

        var projectName = Path.GetFileName(projectPath);
        
        return new ProjectInfo
        {
            Name = projectName,
            Path = projectPath
        };
    }

    public async Task<bool> IsInsideExistingProjectAsync(string path, CancellationToken cancellationToken = default)
    {
        var currentPath = Path.GetDirectoryName(Path.GetFullPath(path));
        
        while (!string.IsNullOrEmpty(currentPath))
        {
            if (await IsValidProjectAsync(currentPath, cancellationToken))
            {
                return true;
            }
            
            // Stop at git repository boundary
            if (Directory.Exists(Path.Combine(currentPath, ".git")))
            {
                break;
            }
            
            var parent = Directory.GetParent(currentPath);
            if (parent == null)
            {
                break;
            }
            
            currentPath = parent.FullName;
        }
        
        return false;
    }

    private async Task ValidateProjectCreationAsync(string projectPath, CancellationToken cancellationToken)
    {
        if (Directory.Exists(projectPath))
        {
            throw new InvalidOperationException($"Project directory already exists: {projectPath}");
        }
        
        if (await IsInsideExistingProjectAsync(projectPath, cancellationToken))
        {
            throw new InvalidOperationException($"Cannot create project inside an existing DbReactor project. Current location is within an existing project structure.");
        }
    }

    private void CreateProjectDirectory(string projectPath)
    {
        Directory.CreateDirectory(projectPath);
        _logger.LogDebug("Created project directory: {ProjectPath}", projectPath);
    }

    private void CreateProjectStructure(string projectPath)
    {
        // Create simple upgrades/downgrades structure directly in project
        var upgradesPath = Path.Combine(projectPath, "upgrades");
        var downgradesPath = Path.Combine(projectPath, "downgrades");
        
        Directory.CreateDirectory(upgradesPath);
        Directory.CreateDirectory(downgradesPath);
        _logger.LogDebug("Created project subdirectories: upgrades and downgrades");
    }

    private async Task SaveProjectConfiguration(string projectPath, string projectName, CliOptions defaultOptions, CancellationToken cancellationToken)
    {
        // Only create config file if user explicitly wants one or has custom settings
        if (ShouldCreateConfigFile(defaultOptions))
        {
            var projectOptions = CreateProjectOptions(defaultOptions, "upgrades", "downgrades");
            var configPath = Path.Combine(projectPath, ProjectConfigFileName);
            
            await _configurationService.SaveConfigurationAsync(configPath, projectOptions, cancellationToken);
            _logger.LogDebug("Created project configuration file");
        }
        else
        {
            _logger.LogDebug("Skipped creating configuration file - using defaults");
        }
    }

    private static CliOptions CreateProjectOptions(CliOptions defaultOptions, string upgradesPath, string downgradesPath)
    {
        return new CliOptions
        {
            ConnectionString = defaultOptions.ConnectionString,
            Provider = defaultOptions.Provider,
            UpgradesPath = upgradesPath,
            DowngradesPath = downgradesPath,
            EnsureDatabase = defaultOptions.EnsureDatabase,
            EnsureDirectories = defaultOptions.EnsureDirectories,
            LogLevel = defaultOptions.LogLevel,
            TimeoutSeconds = defaultOptions.TimeoutSeconds,
            Variables = defaultOptions.Variables
        };
    }

    private void CleanupFailedProject(string projectPath)
    {
        if (Directory.Exists(projectPath))
        {
            try
            {
                Directory.Delete(projectPath, true);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up project directory after creation failure");
            }
        }
    }

    private async Task<IEnumerable<ProjectInfo>> GetRegisteredProjectsAsync(CancellationToken cancellationToken)
    {
        var projects = new List<ProjectInfo>();
        
        try
        {
            await _projectRegistryService.ValidateAndCleanupRegistryAsync(cancellationToken);
            var registeredProjects = await _projectRegistryService.GetRegisteredProjectsAsync(cancellationToken);
            
            foreach (var registered in registeredProjects.Where(p => p.IsValid))
            {
                projects.Add(new ProjectInfo
                {
                    Name = registered.Name,
                    Path = registered.Path
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading registered projects");
        }
        
        return projects;
    }
    
    private async Task<ProjectInfo?> FindProjectInCurrentContextAsync(CancellationToken cancellationToken)
    {
        var currentPath = Directory.GetCurrentDirectory();
        
        // Walk up the directory tree looking for a project
        while (!string.IsNullOrEmpty(currentPath))
        {
            if (await IsValidProjectAsync(currentPath, cancellationToken))
            {
                return new ProjectInfo
                {
                    Name = Path.GetFileName(currentPath),
                    Path = currentPath
                };
            }
            
            // Stop at git repository boundary or system root
            if (Directory.Exists(Path.Combine(currentPath, ".git")) || 
                currentPath == Path.GetPathRoot(currentPath))
            {
                break;
            }
            
            var parent = Directory.GetParent(currentPath);
            currentPath = parent?.FullName;
        }
        
        return null;
    }
    
    private async Task<IEnumerable<ProjectInfo>> ScanWorkspaceDirectoriesAsync(CancellationToken cancellationToken)
    {
        var projects = new List<ProjectInfo>();
        
        try
        {
            var registry = await _projectRegistryService.LoadRegistryAsync(cancellationToken);
            
            foreach (var workspace in registry.WorkspaceDirectories)
            {
                if (Directory.Exists(workspace))
                {
                    var workspaceProjects = await ScanDirectoryForProjectsAsync(workspace, cancellationToken);
                    projects.AddRange(workspaceProjects);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning workspace directories");
        }
        
        return projects;
    }
    
    private async Task<IEnumerable<ProjectInfo>> ScanDirectoryForProjectsAsync(string searchPath, CancellationToken cancellationToken)
    {
        var projects = new List<ProjectInfo>();
        
        try
        {
            // Look for directories with migration structure patterns
            var directories = Directory.GetDirectories(searchPath, "*", SearchOption.AllDirectories);
            var candidates = new HashSet<string>();
            
            foreach (var directory in directories)
            {
                if (HasMigrationStructure(directory))
                {
                    var projectRoot = GetProjectRoot(directory);
                    if (projectRoot != null)
                    {
                        candidates.Add(projectRoot);
                    }
                }
            }
            
            foreach (var candidatePath in candidates)
            {
                var projectInfo = await GetProjectInfoAsync(candidatePath, cancellationToken);
                if (projectInfo != null)
                {
                    projects.Add(projectInfo);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning directory for projects: {SearchPath}", searchPath);
        }
        
        return projects;
    }
    
    private async Task RegisterNewProjectAsync(string projectName, string projectPath, CancellationToken cancellationToken)
    {
        try
        {
            await _projectRegistryService.RegisterProjectAsync(projectName, projectPath, 
                $"Created on {DateTime.Now:yyyy-MM-dd HH:mm}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register new project in registry");
        }
    }
    
    private static bool HasMigrationStructure(string path)
    {
        // Pattern 1: upgrades/ and downgrades/ in same directory
        var upgradesPath = Path.Combine(path, "upgrades");
        var downgradesPath = Path.Combine(path, "downgrades");
        
        if (Directory.Exists(upgradesPath) && Directory.Exists(downgradesPath))
        {
            return true;
        }
        
        // Pattern 2: Scripts/upgrades/ and Scripts/downgrades/
        var scriptsUpgradesPath = Path.Combine(path, "Scripts", "upgrades");
        var scriptsDowngradesPath = Path.Combine(path, "Scripts", "downgrades");
        
        if (Directory.Exists(scriptsUpgradesPath) && Directory.Exists(scriptsDowngradesPath))
        {
            return true;
        }
        
        return false;
    }
    
    private static string? GetProjectRoot(string migrationDirectory)
    {
        // If this directory has both upgrades and downgrades, it's the project root
        if (HasMigrationStructure(migrationDirectory))
        {
            return migrationDirectory;
        }
        
        // If this is "upgrades" or "downgrades", check parent
        var dirName = Path.GetFileName(migrationDirectory);
        if (dirName is "upgrades" or "downgrades")
        {
            var parentDir = Directory.GetParent(migrationDirectory)?.FullName;
            if (parentDir != null && HasMigrationStructure(parentDir))
            {
                return parentDir;
            }
            
            // Check if parent is "Scripts" directory
            if (parentDir != null && Path.GetFileName(parentDir) == "Scripts")
            {
                var grandParentDir = Directory.GetParent(parentDir)?.FullName;
                if (grandParentDir != null && HasMigrationStructure(grandParentDir))
                {
                    return grandParentDir;
                }
            }
        }
        
        return null;
    }

    private static bool ShouldCreateConfigFile(CliOptions options)
    {
        // Only create config file if user has non-default settings
        return options.EnsureDatabase ||
               options.LogLevel != Microsoft.Extensions.Logging.LogLevel.Information ||
               options.TimeoutSeconds != 30 ||
               (options.Variables?.Any() == true);
    }
}