using System.CommandLine;
using DbReactor.CLI.Commands;
using DbReactor.CLI.Configuration;
using DbReactor.CLI.Services;
using DbReactor.CLI.Services.Interactive;
using DbReactor.CLI.Services.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace DbReactor.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var services = ConfigureServices();
            var serviceProvider = services.BuildServiceProvider();

            // Interactive mode: No arguments provided at all
            if (args.Length == 0)
            {
                var interactiveService = serviceProvider.GetRequiredService<IInteractiveService>();
                return await interactiveService.RunInteractiveSessionAsync();
            }

            // Traditional command line mode: Arguments provided
            var rootCommand = new RootCommand("DbReactor CLI - Database Migration Framework")
            {
                Name = "dbreactor"
            };

            var commandFactory = serviceProvider.GetRequiredService<ICommandFactory>();
            
            rootCommand.AddCommand(commandFactory.CreateMigrateCommand());
            rootCommand.AddCommand(commandFactory.CreateStatusCommand());
            rootCommand.AddCommand(commandFactory.CreateRollbackCommand());
            rootCommand.AddCommand(commandFactory.CreateInitCommand());
            rootCommand.AddCommand(commandFactory.CreateCreateScriptCommand());
            rootCommand.AddCommand(commandFactory.CreateValidateCommand());
            rootCommand.AddCommand(serviceProvider.GetRequiredService<ProjectsCommand>());
            rootCommand.AddCommand(serviceProvider.GetRequiredService<VariablesCommand>());

            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("dbreactor.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("DBREACTOR_")
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<ICliConfigurationService, CliConfigurationService>();
        services.AddSingleton<IOutputService, OutputService>();
        services.AddSingleton<ICommandFactory, CommandFactory>();
        services.AddSingleton<IProviderConfigurationFactory, ProviderConfigurationFactory>();

        services.AddTransient<MigrateCommand>();
        services.AddTransient<StatusCommand>();
        services.AddTransient<RollbackCommand>();
        services.AddTransient<InitCommand>();
        services.AddTransient<CreateScriptCommand>();
        services.AddTransient<ValidateCommand>();
        services.AddTransient<ProjectsCommand>();
        services.AddTransient<VariablesCommand>();
        services.AddSingleton<IMigrationService, MigrationService>();
        services.AddSingleton<IRollbackService, RollbackService>();
        services.AddSingleton<IUserInteractionService, UserInteractionService>();
        services.AddSingleton<IProjectManagementService, ProjectManagementService>();
        services.AddSingleton<IProjectRegistryService, ProjectRegistryService>();
        services.AddSingleton<IVariableManagementService, VariableManagementService>();
        services.AddSingleton<IVariableEncryptionService, VariableEncryptionService>();
        services.AddSingleton<IDirectoryService, DirectoryService>();
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<IScriptTemplateService, ScriptTemplateService>();

        // Validation services
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        services.AddSingleton<ICliOptionsValidator, CliOptionsValidator>();
        services.AddSingleton<IPathValidator, PathValidator>();
        services.AddSingleton<IConfigurationBuildValidator, ConfigurationBuildValidator>();

        // Interactive services
        services.AddSingleton<IInteractiveService, InteractiveService>();
        services.AddSingleton<IInteractiveMenuService, InteractiveMenuService>();
        services.AddSingleton<IInteractiveConfigurationCollector, InteractiveConfigurationCollector>();
        services.AddSingleton<ICommandParameterCollector, CommandParameterCollector>();
        services.AddSingleton<ICommandExecutor, CommandExecutor>();

        return services;
    }
}
