using DbReactor.CLI.Constants;
using DbReactor.CLI.Services.Interactive;
using Spectre.Console;

namespace DbReactor.CLI.Services;

public class InteractiveService : IInteractiveService
{
    private readonly IInteractiveMenuService _menuService;
    private readonly IInteractiveConfigurationCollector _configurationCollector;
    private readonly ICommandParameterCollector _parameterCollector;
    private readonly ICommandExecutor _commandExecutor;

    public InteractiveService(
        IInteractiveMenuService menuService,
        IInteractiveConfigurationCollector configurationCollector,
        ICommandParameterCollector parameterCollector,
        ICommandExecutor commandExecutor)
    {
        _menuService = menuService;
        _configurationCollector = configurationCollector;
        _parameterCollector = parameterCollector;
        _commandExecutor = commandExecutor;
    }

    public async Task<int> RunInteractiveSessionAsync(CancellationToken cancellationToken = default)
    {
        _menuService.ShowWelcomeBanner();

        // Collect base configuration first
        var baseConfiguration = await _configurationCollector.CollectBaseConfigurationAsync();

        while (!cancellationToken.IsCancellationRequested)
        {
            var selectedCommand = _menuService.ShowCommandMenu();
            
            if (selectedCommand == "exit")
            {
                AnsiConsole.MarkupLine("[green]Goodbye![/]");
                return ExitCodes.Success;
            }

            var parameters = await _parameterCollector.CollectParametersAsync(selectedCommand, baseConfiguration);
            var exitCode = await _commandExecutor.ExecuteCommandAsync(selectedCommand, parameters);
            
            HandleCommandResult(exitCode);
            
            WaitForUserToContinue();
        }

        return ExitCodes.Success;
    }

    private static void HandleCommandResult(int exitCode)
    {
        if (exitCode != ExitCodes.Success)
        {
            AnsiConsole.MarkupLine($"[red]Command failed with exit code: {exitCode}[/]");
        }
    }

    private static void WaitForUserToContinue()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
        Console.Clear();
    }
}