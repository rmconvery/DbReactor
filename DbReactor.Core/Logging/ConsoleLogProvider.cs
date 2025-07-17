using DbReactor.Core.Logging;
using System;

namespace DbReactor.Core.Logging
{
    /// <summary>
    /// Console-based implementation of ILogProvider
    /// </summary>
    public class ConsoleLogProvider : ILogProvider
        {
            public void WriteInformation(string format, params object[] args)
            {
                string message = args?.Length > 0 ? string.Format(format, args) : format;
                Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
            }

            public void WriteError(string format, params object[] args)
            {
                string message = args?.Length > 0 ? string.Format(format, args) : format;
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                Console.ForegroundColor = originalColor;
            }

            public void WriteWarning(string format, params object[] args)
            {
                string message = args?.Length > 0 ? string.Format(format, args) : format;
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                Console.ForegroundColor = originalColor;
            }
    }
}
