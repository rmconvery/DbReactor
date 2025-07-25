namespace DbReactor.CLI.Models;

public class RollbackOptions
{
    public bool RollbackLast { get; set; }
    public bool RollbackAll { get; set; }
}

public enum RollbackMode
{
    Last,
    All
}