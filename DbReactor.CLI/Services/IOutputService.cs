using DbReactor.Core.Models;

namespace DbReactor.CLI.Services;

public interface IOutputService
{
    void WriteSuccess(string message);
    void WriteError(string message, Exception? exception = null);
    void WriteWarning(string message);
    void WriteInfo(string message);
    void WriteTable<T>(IEnumerable<T> data, string title = "");
    void WriteProgress(string message, Func<Task> operation);
    void WriteMigrationStatus(IEnumerable<RunPreviewResult> migrationResults);
    void WriteMigrationResult(DbReactorResult result);
}