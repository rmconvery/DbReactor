using System.CommandLine;
using DbReactor.CLI.Configuration;
using DbReactor.CLI.Constants;
using DbReactor.CLI.Models;
using DbReactor.CLI.Services;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Commands;

public class InitCommand : BaseCommand
{
    private readonly IProjectManagementService _projectManagementService;

    public InitCommand(
        ICliConfigurationService configurationService,
        IOutputService outputService,
        IProjectManagementService projectManagementService,
        ILogger<InitCommand> logger)
        : base(configurationService, outputService, logger)
    {
        _projectManagementService = projectManagementService;
    }

    public override Command BuildCommand()
    {
        var command = new Command("init", "Initialize a new DbReactor project");

        var projectNameArgument = new Argument<string>("project-name", "Name of the project to create");
        var outputPathOption = new Option<string?>("--output", "Directory where the project should be created (defaults to current directory)");
        var connectionStringOption = CreateConnectionStringOption();

        command.AddArgument(projectNameArgument);
        command.AddOption(outputPathOption);
        command.AddOption(connectionStringOption);

        command.SetHandler(async (context) =>
        {
            var projectName = context.ParseResult.GetValueForArgument(projectNameArgument);
            var outputPath = context.ParseResult.GetValueForOption(outputPathOption) ?? Directory.GetCurrentDirectory();
            var connectionString = context.ParseResult.GetValueForOption(connectionStringOption);

            var result = await ExecuteWithErrorHandling(async () =>
            {
                return await CreateProjectAsync(projectName, outputPath, connectionString, context.GetCancellationToken());
            }, context.GetCancellationToken());

            context.ExitCode = result.ExitCode;
        });

        return command;
    }

    private async Task<CommandResult> CreateProjectAsync(string projectName, string outputPath, string? connectionString, CancellationToken cancellationToken)
    {
        try
        {
            ValidateInputs(projectName, outputPath, connectionString);

            var defaultOptions = CreateDefaultOptions(connectionString!);
            var projectInfo = await _projectManagementService.CreateProjectAsync(projectName, outputPath, defaultOptions, cancellationToken);

            DisplaySuccessMessage(projectInfo);
            return CommandResult.Ok($"Project '{projectName}' created successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create project: {ProjectName}", projectName);
            return CommandResult.Error($"Failed to create project: {ex.Message}", ex);
        }
    }

    private static void ValidateInputs(string projectName, string outputPath, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name is required", nameof(projectName));
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required", nameof(connectionString));
        }

        if (!Directory.Exists(outputPath))
        {
            throw new DirectoryNotFoundException($"Output directory does not exist: {outputPath}");
        }
    }

    private static CliOptions CreateDefaultOptions(string connectionString)
    {
        return new CliOptions
        {
            ConnectionString = connectionString,
            Provider = "sqlserver",
            LogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
            TimeoutSeconds = 30,
            EnsureDatabase = false,
            EnsureDirectories = true
        };
    }

    private void DisplaySuccessMessage(ProjectInfo projectInfo)
    {
        OutputService.WriteSuccess($"✓ Project '{projectInfo.Name}' created successfully!");
        OutputService.WriteInfo($"Project path: {projectInfo.Path}");
        OutputService.WriteInfo("Project structure:");
        OutputService.WriteInfo("  ├── upgrades/ (SQL upgrade scripts)");
        OutputService.WriteInfo("  └── downgrades/ (SQL downgrade scripts)");
        OutputService.WriteInfo("");
        OutputService.WriteInfo("Next steps:");
        OutputService.WriteInfo($"  1. cd \"{projectInfo.Path}\"");
        OutputService.WriteInfo("  2. dbreactor create-script <script-name>");
        OutputService.WriteInfo("  3. dbreactor migrate");
    }
}