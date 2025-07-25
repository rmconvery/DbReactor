using DbReactor.CLI.Constants;

namespace DbReactor.CLI.Models;

public record CommandResult(bool Success, string Message = "", Exception? Exception = null, int ExitCode = ExitCodes.Success)
{
    public static CommandResult Ok(string message = "") => new(true, message, null, ExitCodes.Success);
    public static CommandResult Error(string message, Exception? exception = null, int exitCode = ExitCodes.GeneralError) => 
        new(false, message, exception, exitCode);
    public static CommandResult ConfigurationError(string message, Exception? exception = null) => 
        new(false, message, exception, ExitCodes.ConfigurationError);
    public static CommandResult MigrationError(string message, Exception? exception = null) => 
        new(false, message, exception, ExitCodes.MigrationError);
    public static CommandResult ValidationError(string message, Exception? exception = null) => 
        new(false, message, exception, ExitCodes.ValidationError);
    public static CommandResult UserCancelled(string message = "Operation cancelled by user") => 
        new(false, message, null, ExitCodes.UserCancelled);
}