using DbReactor.Core.Models;
using Spectre.Console;

namespace DbReactor.CLI.Services;

public class OutputService : IOutputService
{
    public void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");
    }

    public void WriteError(string message, Exception? exception = null)
    {
        AnsiConsole.MarkupLine($"[red]✗[/] {message}");
        if (exception != null)
        {
            AnsiConsole.WriteException(exception);
        }
    }

    public void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]⚠[/] {message}");
    }

    public void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue]ℹ[/] {message}");
    }

    public void WriteTable<T>(IEnumerable<T> data, string title = "")
    {
        var table = new Table();
        if (!string.IsNullOrEmpty(title))
        {
            table.Title = new TableTitle(title);
        }

        var properties = typeof(T).GetProperties();
        foreach (var prop in properties)
        {
            table.AddColumn(prop.Name);
        }

        foreach (var item in data)
        {
            var values = properties.Select(p => p.GetValue(item)?.ToString() ?? "").ToArray();
            table.AddRow(values);
        }

        AnsiConsole.Write(table);
    }

    public void WriteProgress(string message, Func<Task> operation)
    {
        AnsiConsole.Progress()
            .Start(ctx =>
            {
                var task = ctx.AddTask(message);
                var result = operation();
                task.MaxValue = 100;
                task.Value = 100;
                return result;
            });
    }

    public void WriteMigrationStatus(IEnumerable<RunPreviewResult> migrationResults)
    {
        var table = new Table()
            .AddColumn("Migration")
            .AddColumn("Status");

        foreach (var result in migrationResults)
        {
            var status = result.AlreadyExecuted ? "[green]Executed[/]" : "[yellow]Pending[/]";
            table.AddRow(result.MigrationName, status);
        }

        AnsiConsole.Write(table);
    }

    public void WriteMigrationResult(DbReactorResult result)
    {
        if (result.Successful)
        {
            WriteSuccess($"Migration completed successfully. Executed {result.Scripts.Count} migrations.");
        }
        else
        {
            WriteError("Migration failed.", result.Error);
        }

        if (result.Scripts.Any())
        {
            WriteInfo("Executed migrations:");
            foreach (var migration in result.Scripts)
            {
                AnsiConsole.MarkupLine($"  • {migration.Script.Name}");
            }
        }
    }
}