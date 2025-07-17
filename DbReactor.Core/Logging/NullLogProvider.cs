using DbReactor.Core.Logging;

namespace DbReactor.Core.Logging
{
    /// <summary>
    /// Null object pattern implementation of ILogProvider that does nothing
    /// </summary>
    public class NullLogProvider : ILogProvider
    {
        public void WriteInformation(string format, params object[] args)
        {
            // Do nothing
        }

        public void WriteError(string format, params object[] args)
        {
            // Do nothing
        }

        public void WriteWarning(string format, params object[] args)
        {
            // Do nothing
        }
    }
}

