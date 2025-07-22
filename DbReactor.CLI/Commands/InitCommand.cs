using System.CommandLine;
using DbReactor.CLI.Configuration;
using DbReactor.CLI.Models;
using DbReactor.CLI.Services;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Commands;

public class InitCommand : BaseCommand
{
    private readonly IProjectInitializationService _projectInitializationService;

    public InitCommand(
        ICliConfigurationService configurationService,
        IOutputService outputService,
        IProjectInitializationService projectInitializationService,
        ILogger<InitCommand> logger) 
        : base(configurationService, outputService, logger)
    {
        _projectInitializationService = projectInitializationService;
    }

    public override Command BuildCommand()
    {
        var command = new Command("init", "Initialize a new DbReactor project structure");

        var pathOption = new Option<string>(
            new[] { "--path", "-p" }, 
            () => Directory.GetCurrentDirectory(),
            "Target directory for project initialization");

        command.AddOption(pathOption);

        command.SetHandler(async (context) =>
        {
            var targetPath = context.ParseResult.GetValueForOption(pathOption)!;

            var result = await ExecuteWithErrorHandling(async () =>
            {
                return await InitializeProjectAsync(targetPath, context.GetCancellationToken());
            }, context.GetCancellationToken());

            context.ExitCode = result.ExitCode;
        });

        return command;
    }

    private async Task<CommandResult> InitializeProjectAsync(string targetPath, CancellationToken cancellationToken)
    {
        try
        {
            ValidateTargetPath(targetPath);
            
            OutputService.WriteInfo($"Initializing DbReactor project structure in: {targetPath}");

            var result = await _projectInitializationService.InitializeProjectAsync(targetPath, cancellationToken);
            
            if (result.Success)
            {
                DisplayInitializationSuccess(targetPath);
                OutputService.WriteSuccess("Project initialization completed successfully!");
            }
            else
            {
                OutputService.WriteError("Project initialization failed", result.Exception);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Project initialization failed");
            return CommandResult.Error("Project initialization failed", ex);
        }
    }

    private static void ValidateTargetPath(string targetPath)
    {
        if (!Path.IsPathRooted(targetPath))
        {
            throw new ArgumentException($"Path must be absolute: {targetPath}");
        }
    }

    private void DisplayInitializationSuccess(string targetPath)
    {
        OutputService.WriteInfo("Created project structure:");
        OutputService.WriteInfo($"  📁 {Path.Combine(targetPath, "Scripts")}");
        OutputService.WriteInfo($"  📁 {Path.Combine(targetPath, "Scripts", "upgrades")}");
        OutputService.WriteInfo($"  📁 {Path.Combine(targetPath, "Scripts", "downgrades")}");

        OutputService.WriteInfo("");
        OutputService.WriteInfo("Next steps:");
        OutputService.WriteInfo("1. Create your first migration with: dbreactor create-script");
        OutputService.WriteInfo("2. Run migrations with: dbreactor migrate --connection-string \"your-connection-string\"");
    }
}