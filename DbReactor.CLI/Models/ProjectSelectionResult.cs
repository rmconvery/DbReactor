namespace DbReactor.CLI.Models;

public abstract record ProjectSelectionResult
{
    public sealed record UseExisting(ProjectInfo ProjectInfo) : ProjectSelectionResult;
    public sealed record CreateNew(string ProjectName, string OutputPath) : ProjectSelectionResult;
    public sealed record UseManual : ProjectSelectionResult;
}