namespace DbReactor.CLI.Models;

public class VariableMetadata
{
    public bool IsSensitive { get; set; }
    public DateTime LastModified { get; set; } = DateTime.Now;
    public string? Description { get; set; }
}