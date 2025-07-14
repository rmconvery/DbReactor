namespace DbReactor.Core.Logging
{
    /// <summary>
    /// Provides logging capabilities
    /// </summary>
    public interface ILogProvider
    {
        void WriteInformation(string format, params object[] args);
        void WriteError(string format, params object[] args);
        void WriteWarning(string format, params object[] args);
    }
}