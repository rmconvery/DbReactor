using System;
using System.Collections.Generic;

namespace DbReactor.Core.Models
{
    /// <summary>
    /// Result of database reactor operations
    /// </summary>
    public class DbReactorResult
    {
        public bool Successful { get; set; }
        public Exception Error { get; set; }
        public List<MigrationResult> Scripts { get; set; } = new List<MigrationResult>();
        public string ErrorMessage { get; set; }
    }
}
