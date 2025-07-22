using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Models;

public class CliOptions
{
    public string? ConnectionString { get; set; }
    public string? Provider { get; set; } = "sqlserver";
    public string? UpgradesPath { get; set; }
    public string? DowngradesPath { get; set; }
    public string? ConfigFile { get; set; }
    public bool Verbose { get; set; }
    public bool DryRun { get; set; }
    public bool Force { get; set; }
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public int? TimeoutSeconds { get; set; }
    public bool EnsureDatabase { get; set; }
    public bool EnsureDirectories { get; set; }
    public Dictionary<string, string> Variables { get; set; } = new();
}