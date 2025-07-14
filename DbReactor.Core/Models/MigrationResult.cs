using DbReactor.Core.Abstractions;
using System;

namespace DbReactor.Core.Models
{
    /// <summary>
    /// Represents the result of a script execution
    /// </summary>
    public class MigrationResult
    {
        public bool Successful { get; set; }
        public Exception Error { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public IScript Script { get; set; }
    }
}