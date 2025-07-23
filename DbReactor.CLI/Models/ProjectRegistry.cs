using System.Collections.Generic;

namespace DbReactor.CLI.Models;

public class ProjectRegistry
{
    public Dictionary<string, RegisteredProject> Projects { get; set; } = new();
    public List<string> WorkspaceDirectories { get; set; } = new();
    public DateTime LastScan { get; set; } = DateTime.MinValue;
}

public class RegisteredProject
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public DateTime LastAccessed { get; set; } = DateTime.Now;
    public bool IsValid { get; set; } = true;
    public string? Description { get; set; }
}