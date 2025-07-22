namespace DbReactor.CLI.Models;

public class ScriptTemplate
{
    public string Name { get; set; } = string.Empty;
    public ScriptType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool CreateDowngrade { get; set; }
    public string? DowngradeContent { get; set; }
}

public enum ScriptType
{
    Sql
}