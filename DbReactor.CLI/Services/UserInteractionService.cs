using Spectre.Console;

namespace DbReactor.CLI.Services;

public class UserInteractionService : IUserInteractionService
{
    public async Task<bool> ConfirmActionAsync(string message, CancellationToken cancellationToken = default)
    {
        return AnsiConsole.Confirm(message);
    }

    public async Task<string?> PromptForInputAsync(string message, CancellationToken cancellationToken = default)
    {
        return AnsiConsole.Ask<string>(message);
    }
}