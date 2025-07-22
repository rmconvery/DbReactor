namespace DbReactor.CLI.Services;

public interface IUserInteractionService
{
    Task<bool> ConfirmActionAsync(string message, CancellationToken cancellationToken = default);
    Task<string?> PromptForInputAsync(string message, CancellationToken cancellationToken = default);
}