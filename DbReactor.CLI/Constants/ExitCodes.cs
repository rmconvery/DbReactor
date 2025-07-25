namespace DbReactor.CLI.Constants;

public static class ExitCodes
{
    public const int Success = 0;
    public const int GeneralError = 1;
    public const int ConfigurationError = 2;
    public const int MigrationError = 3;
    public const int ValidationError = 4;
    public const int UserCancelled = 5;
}