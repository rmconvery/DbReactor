using Spectre.Console;

namespace DbReactor.CLI.Services.Interactive;

public class InteractiveMenuService : IInteractiveMenuService
{
    private static readonly (string Command, string Description)[] AvailableCommands = new[]
    {
        ("variables", "Manage configuration variables"),
        ("migrate", "Run database migrations"),
        ("status", "Show migration status and history"),
        ("rollback", "Rollback migrations"),
        ("init", "Initialize new project"),
        ("create-script", "Create new migration script"),
        ("validate", "Validate configuration"),
        ("exit", "Exit DbReactor")
    };

    public void ShowWelcomeBanner()
    {
        Console.Clear();
        
        var panel = new Panel(new FigletText("DbReactor").Color(Color.Blue))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Header = new PanelHeader("[bold white]Database Migration Framework[/]")
        };
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public string ShowCommandMenu()
    {
        var table = new Table()
        {
            Border = TableBorder.Rounded
        };
        
        table.AddColumn(new TableColumn("[bold]Command[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Description[/]"));
        
        foreach (var (command, description) in AvailableCommands)
        {
            var commandMarkup = command == "exit" 
                ? $"[red]{command}[/]" 
                : $"[green]{command}[/]";
            
            table.AddRow(commandMarkup, description);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold blue]Choose a command:[/]")
                .AddChoices(AvailableCommands.Select(c => c.Command))
                .UseConverter(cmd => 
                {
                    var commandInfo = AvailableCommands.First(c => c.Command == cmd);
                    return $"{commandInfo.Command} - {commandInfo.Description}";
                }));

        return selection;
    }
}