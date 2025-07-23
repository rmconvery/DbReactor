using DbReactor.CLI.Models;
using DbReactor.CLI.Configuration;
using Spectre.Console;

namespace DbReactor.CLI.Services.Interactive;

public class InteractiveConfigurationCollector : IInteractiveConfigurationCollector
{
    private readonly IProjectManagementService _projectManagementService;
    private readonly ICliConfigurationService _configurationService;
    private readonly IVariableManagementService _variableManagementService;

    public InteractiveConfigurationCollector(
        IProjectManagementService projectManagementService,
        ICliConfigurationService configurationService,
        IVariableManagementService variableManagementService)
    {
        _projectManagementService = projectManagementService;
        _configurationService = configurationService;
        _variableManagementService = variableManagementService;
    }

    public async Task<CliOptions> CollectBaseConfigurationAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]DbReactor Project Setup[/]");
        AnsiConsole.WriteLine();

        var projectSelection = await HandleProjectSelection();
        
        return projectSelection switch
        {
            ProjectSelectionResult.UseExisting existingProject => await LoadExistingProjectConfiguration(existingProject.ProjectInfo),
            ProjectSelectionResult.CreateNew newProject => await CreateNewProjectConfiguration(newProject.ProjectName, newProject.OutputPath),
            ProjectSelectionResult.UseManual => await CollectManualConfiguration(),
            _ => throw new InvalidOperationException("Unknown project selection result")
        };
    }

    private async Task<ProjectSelectionResult> HandleProjectSelection()
    {
        AnsiConsole.MarkupLine("[dim]Choose how you want to work with DbReactor:[/]");
        AnsiConsole.WriteLine();

        var availableProjects = await _projectManagementService.ScanForProjectsAsync(Directory.GetCurrentDirectory());
        var projectList = availableProjects.ToList();

        var choices = new List<string> { "Create new project", "Use manual configuration" };
        
        if (projectList.Any())
        {
            choices.Insert(1, "Use existing project");
        }

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Project setup:[/]")
                .AddChoices(choices));

        return selection switch
        {
            "Use existing project" when projectList.Any() => await SelectExistingProject(projectList),
            "Create new project" => await CreateNewProject(),
            "Use manual configuration" => new ProjectSelectionResult.UseManual(),
            _ => throw new InvalidOperationException($"Unknown selection: {selection}")
        };
    }

    private async Task<ProjectSelectionResult> SelectExistingProject(List<ProjectInfo> projects)
    {
        AnsiConsole.MarkupLine("[yellow]Available Projects:[/]");
        
        var selectedProject = AnsiConsole.Prompt(
            new SelectionPrompt<ProjectInfo>()
                .Title("[blue]Select a project:[/]")
                .AddChoices(projects)
                .UseConverter(p => $"{p.Name} ([dim]{p.Path}[/])"));

        return new ProjectSelectionResult.UseExisting(selectedProject);
    }

    private async Task<ProjectSelectionResult> CreateNewProject()
    {
        var projectName = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Project name:[/]")
                .ValidationErrorMessage("[red]Project name is required[/]")
                .Validate(name => !string.IsNullOrWhiteSpace(name) 
                    ? Spectre.Console.ValidationResult.Success() 
                    : Spectre.Console.ValidationResult.Error("[red]Project name cannot be empty[/]")));

        var outputPath = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]Output directory (leave empty for current directory):[/]")
                .DefaultValue(Directory.GetCurrentDirectory())
                .AllowEmpty());

        return new ProjectSelectionResult.CreateNew(projectName, outputPath);
    }

    private async Task<CliOptions> LoadExistingProjectConfiguration(ProjectInfo projectInfo)
    {
        AnsiConsole.MarkupLine($"[green]✓ Using existing project: {projectInfo.Name}[/]");
        AnsiConsole.MarkupLine($"[dim]Path: {projectInfo.Path}[/]");
        
        // Change to project directory
        Directory.SetCurrentDirectory(projectInfo.Path);
        
        // Load configuration from project
        var configPath = Path.Combine(projectInfo.Path, "dbreactor.json");
        return await _configurationService.LoadConfigurationAsync(configPath);
    }

    private async Task<CliOptions> CreateNewProjectConfiguration(string projectName, string outputPath)
    {
        AnsiConsole.MarkupLine("[yellow]Setting up new project configuration...[/]");
        
        var connectionString = CollectConnectionString();
        var provider = CollectDatabaseProvider();
        
        var defaultOptions = new CliOptions
        {
            ConnectionString = connectionString,
            Provider = provider,
            LogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
            TimeoutSeconds = 30,
            EnsureDatabase = false,
            EnsureDirectories = true
        };

        await CollectCommonOptions(defaultOptions);

        var projectInfo = await _projectManagementService.CreateProjectAsync(projectName, outputPath, defaultOptions);
        
        AnsiConsole.MarkupLine($"[green]✓ Created project: {projectInfo.Name}[/]");
        AnsiConsole.MarkupLine($"[dim]Path: {projectInfo.Path}[/]");
        
        // Change to project directory
        Directory.SetCurrentDirectory(projectInfo.Path);
        
        return defaultOptions;
    }

    private async Task<CliOptions> CollectManualConfiguration()
    {
        AnsiConsole.MarkupLine("[yellow]Manual Configuration[/]");
        AnsiConsole.MarkupLine("[dim]Let's collect the basic configuration that will be used for all commands.[/]");
        AnsiConsole.WriteLine();

        var options = new CliOptions();

        options.ConnectionString = CollectConnectionString();
        options.Provider = CollectDatabaseProvider();
        CollectScriptPaths(options);
        await CollectCommonOptions(options);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓ Configuration collected successfully![/]");
        AnsiConsole.WriteLine();

        return options;
    }

    private string CollectConnectionString()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Database connection string:[/]")
                .PromptStyle("blue")
                .ValidationErrorMessage("[red]Connection string is required[/]")
                .Validate(connectionString =>
                {
                    if (string.IsNullOrWhiteSpace(connectionString))
                        return Spectre.Console.ValidationResult.Error("[red]Connection string cannot be empty[/]");

                    return Spectre.Console.ValidationResult.Success();
                }));
    }

    private string CollectDatabaseProvider()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Database provider:[/]")
                .AddChoices("sqlserver")
                .UseConverter(provider => provider switch
                {
                    "sqlserver" => "SQL Server",
                    _ => provider
                }));
    }

    private void CollectScriptPaths(CliOptions options)
    {
        AnsiConsole.MarkupLine("[yellow]Script Directory Configuration[/]");
        AnsiConsole.MarkupLine("[dim]Paths can be relative to current directory or absolute.[/]");
        AnsiConsole.WriteLine();

        string upgradesPath = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]Upgrades path (relative/absolute, empty for default './upgrades'):[/]")
                .DefaultValue("")
                .AllowEmpty());

        if (!string.IsNullOrWhiteSpace(upgradesPath))
        {
            options.UpgradesPath = upgradesPath;
        }

        string downgradesPath = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]Downgrades path (relative/absolute, empty for default './downgrades'):[/]")
                .DefaultValue("")
                .AllowEmpty());

        if (!string.IsNullOrWhiteSpace(downgradesPath))
        {
            options.DowngradesPath = downgradesPath;
        }

        options.EnsureDirectories = AnsiConsole.Confirm(
            "[yellow]Create directories if they don't exist?[/]", true);
    }

    private async Task CollectCommonOptions(CliOptions options)
    {
        AnsiConsole.MarkupLine("[yellow]Common Options[/]");

        options.EnsureDatabase = AnsiConsole.Confirm(
            "[yellow]Ensure database exists (create if not found)?[/]", false);

        int timeoutSeconds = AnsiConsole.Prompt(
            new TextPrompt<int>("[blue]Command timeout (seconds):[/]")
                .DefaultValue(30)
                .ValidationErrorMessage("[red]Timeout must be a positive number[/]")
                .Validate(timeout => timeout > 0
                    ? Spectre.Console.ValidationResult.Success()
                    : Spectre.Console.ValidationResult.Error("[red]Timeout must be greater than 0[/]")));

        options.TimeoutSeconds = timeoutSeconds;

        if (AnsiConsole.Confirm("[yellow]Enable verbose logging?[/]", false))
        {
            options.LogLevel = Microsoft.Extensions.Logging.LogLevel.Debug;
        }

        await CollectVariables(options);
    }

    private async Task CollectVariables(CliOptions options)
    {
        AnsiConsole.WriteLine();
        
        if (AnsiConsole.Confirm("[yellow]Configure variables for script substitution?[/]", false))
        {
            AnsiConsole.MarkupLine("[dim]Variables can be used in SQL scripts with ${VariableName} syntax[/]");
            AnsiConsole.MarkupLine("[dim]Example: CREATE TABLE ${Environment}_Users[/]");
            AnsiConsole.WriteLine();

            var variables = await _variableManagementService.ManageVariablesInteractivelyAsync(
                options.Variables, CancellationToken.None);
            
            options.Variables = variables;
        }
    }
}