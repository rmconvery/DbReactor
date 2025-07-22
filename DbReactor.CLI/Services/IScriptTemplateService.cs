using DbReactor.CLI.Models;

namespace DbReactor.CLI.Services;

public interface IScriptTemplateService
{
    Task<CommandResult> CreateScriptAsync(string name, ScriptType type, string? upgradesPath, string? downgradesPath, bool createDowngrade, CancellationToken cancellationToken = default);
}