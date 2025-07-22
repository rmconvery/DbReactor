namespace DbReactor.CLI.Models;

public record ValidationResult(ValidationLevel Level, string Component, string Message)
{
    public bool IsValid => Level != ValidationLevel.Error;

    public static ValidationResult Success(string component, string message) => 
        new(ValidationLevel.Success, component, message);

    public static ValidationResult Warning(string component, string message) => 
        new(ValidationLevel.Warning, component, message);

    public static ValidationResult Error(string component, string message) => 
        new(ValidationLevel.Error, component, message);

    public static ValidationResult Info(string component, string message) => 
        new(ValidationLevel.Info, component, message);
}

public enum ValidationLevel
{
    Success,
    Info,
    Warning,
    Error
}