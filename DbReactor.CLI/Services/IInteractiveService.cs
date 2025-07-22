namespace DbReactor.CLI.Services;

public interface IInteractiveService
{
    Task<int> RunInteractiveSessionAsync(CancellationToken cancellationToken = default);
}