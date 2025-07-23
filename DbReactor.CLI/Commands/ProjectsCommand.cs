using System.CommandLine;
using DbReactor.CLI.Constants;
using DbReactor.CLI.Services;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Commands;

public class ProjectsCommand : Command
{
    private readonly IProjectRegistryService _projectRegistryService;
    private readonly IProjectManagementService _projectManagementService;
    private readonly IOutputService _outputService;
    private readonly ILogger<ProjectsCommand> _logger;

    public ProjectsCommand(
        IProjectRegistryService projectRegistryService,
        IProjectManagementService projectManagementService,
        IOutputService outputService,
        ILogger<ProjectsCommand> logger) : base("projects", "Manage DbReactor projects")
    {
        _projectRegistryService = projectRegistryService;
        _projectManagementService = projectManagementService;
        _outputService = outputService;
        _logger = logger;

        AddCommand(CreateListCommand());
        AddCommand(CreateRegisterCommand());
        AddCommand(CreateUnregisterCommand());
        AddCommand(CreateWorkspaceCommand());
    }

    private Command CreateListCommand()
    {
        var command = new Command("list", "List all known DbReactor projects");
        
        command.SetHandler(async (context) =>
        {
            try
            {
                var projects = await _projectManagementService.ScanForProjectsAsync(string.Empty, context.GetCancellationToken());
                var projectList = projects.ToList();

                if (!projectList.Any())
                {
                    _outputService.WriteInfo("No DbReactor projects found.");
                    _outputService.WriteInfo("Use 'dbreactor init <project-name>' to create a new project.");
                    context.ExitCode = ExitCodes.Success;
                    return;
                }

                _outputService.WriteSuccess($"Found {projectList.Count} DbReactor project(s):");
                _outputService.WriteInfo("");

                foreach (var project in projectList)
                {
                    _outputService.WriteInfo($"  {project.Name}");
                    _outputService.WriteInfo($"    Path: {project.Path}");
                    
                    var isValid = await _projectManagementService.IsValidProjectAsync(project.Path, context.GetCancellationToken());
                    _outputService.WriteInfo($"    Status: {(isValid ? "Valid" : "Invalid")}");
                    _outputService.WriteInfo("");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list projects");
                _outputService.WriteError($"Failed to list projects: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        return command;
    }

    private Command CreateRegisterCommand()
    {
        var command = new Command("register", "Register an existing DbReactor project");
        
        var nameArgument = new Argument<string>("name", "Name to register the project as");
        var pathArgument = new Argument<string>("path", "Path to the project directory");
        var descriptionOption = new Option<string?>("--description", "Optional description for the project");

        command.AddArgument(nameArgument);
        command.AddArgument(pathArgument);
        command.AddOption(descriptionOption);
        
        command.SetHandler(async (context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArgument);
            var path = context.ParseResult.GetValueForArgument(pathArgument);
            var description = context.ParseResult.GetValueForOption(descriptionOption);

            try
            {
                var absolutePath = Path.GetFullPath(path);
                
                if (!Directory.Exists(absolutePath))
                {
                    _outputService.WriteError($"Directory does not exist: {absolutePath}");
                    context.ExitCode = ExitCodes.ConfigurationError;
                    return;
                }

                if (!await _projectManagementService.IsValidProjectAsync(absolutePath, context.GetCancellationToken()))
                {
                    _outputService.WriteError($"Directory is not a valid DbReactor project: {absolutePath}");
                    _outputService.WriteInfo("A valid project should contain 'upgrades' and 'downgrades' directories.");
                    context.ExitCode = ExitCodes.ConfigurationError;
                    return;
                }

                await _projectRegistryService.RegisterProjectAsync(name, absolutePath, description, context.GetCancellationToken());
                
                _outputService.WriteSuccess($"✓ Registered project '{name}' at '{absolutePath}'");
                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register project");
                _outputService.WriteError($"Failed to register project: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        return command;
    }

    private Command CreateUnregisterCommand()
    {
        var command = new Command("unregister", "Unregister a DbReactor project");
        
        var nameArgument = new Argument<string>("name", "Name of the project to unregister");
        command.AddArgument(nameArgument);
        
        command.SetHandler(async (context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArgument);

            try
            {
                var project = await _projectRegistryService.FindProjectByNameAsync(name, context.GetCancellationToken());
                if (project == null)
                {
                    _outputService.WriteError($"Project '{name}' not found in registry");
                    context.ExitCode = ExitCodes.ConfigurationError;
                    return;
                }

                await _projectRegistryService.UnregisterProjectAsync(name, context.GetCancellationToken());
                
                _outputService.WriteSuccess($"✓ Unregistered project '{name}'");
                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister project");
                _outputService.WriteError($"Failed to unregister project: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        return command;
    }

    private Command CreateWorkspaceCommand()
    {
        var workspaceCommand = new Command("workspace", "Manage workspace directories for project discovery");
        
        var addCommand = new Command("add", "Add a workspace directory");
        var pathArgument = new Argument<string>("path", "Path to the workspace directory");
        addCommand.AddArgument(pathArgument);
        addCommand.SetHandler(async (context) =>
        {
            var path = context.ParseResult.GetValueForArgument(pathArgument);
            
            try
            {
                var absolutePath = Path.GetFullPath(path);
                
                if (!Directory.Exists(absolutePath))
                {
                    _outputService.WriteError($"Directory does not exist: {absolutePath}");
                    context.ExitCode = ExitCodes.ConfigurationError;
                    return;
                }

                await _projectRegistryService.AddWorkspaceAsync(absolutePath, context.GetCancellationToken());
                _outputService.WriteSuccess($"✓ Added workspace directory: {absolutePath}");
                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add workspace");
                _outputService.WriteError($"Failed to add workspace: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        var removeCommand = new Command("remove", "Remove a workspace directory");
        var removePathArgument = new Argument<string>("path", "Path to the workspace directory to remove");
        removeCommand.AddArgument(removePathArgument);
        removeCommand.SetHandler(async (context) =>
        {
            var path = context.ParseResult.GetValueForArgument(removePathArgument);
            
            try
            {
                var absolutePath = Path.GetFullPath(path);
                await _projectRegistryService.RemoveWorkspaceAsync(absolutePath, context.GetCancellationToken());
                _outputService.WriteSuccess($"✓ Removed workspace directory: {absolutePath}");
                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove workspace");
                _outputService.WriteError($"Failed to remove workspace: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        var listCommand = new Command("list", "List workspace directories");
        listCommand.SetHandler(async (context) =>
        {
            try
            {
                var registry = await _projectRegistryService.LoadRegistryAsync(context.GetCancellationToken());
                
                if (!registry.WorkspaceDirectories.Any())
                {
                    _outputService.WriteInfo("No workspace directories configured.");
                    context.ExitCode = ExitCodes.Success;
                    return;
                }

                _outputService.WriteInfo("Configured workspace directories:");
                foreach (var workspace in registry.WorkspaceDirectories)
                {
                    var exists = Directory.Exists(workspace);
                    _outputService.WriteInfo($"  {workspace} {(exists ? "" : "(does not exist)")}");
                }
                
                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list workspaces");
                _outputService.WriteError($"Failed to list workspaces: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        workspaceCommand.AddCommand(addCommand);
        workspaceCommand.AddCommand(removeCommand);
        workspaceCommand.AddCommand(listCommand);
        
        return workspaceCommand;
    }
}