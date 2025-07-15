using DbReactor.Core.Execution;
using System.Collections.Generic;

namespace DbReactor.Core.Models.Contexts
{
    /// <summary>
    /// Context object that provides access to database connections and variables for code script execution
    /// </summary>
    public class CodeScriptContext
    {
        /// <summary>
        /// Database connection manager for querying data
        /// </summary>
        public IConnectionManager ConnectionManager { get; }

        /// <summary>
        /// Variables available for use in script generation
        /// </summary>
        public IReadOnlyDictionary<string, string> Variables { get; }

        /// <summary>
        /// Provides a fluent API for accessing variables with type conversion and validation
        /// </summary>
        public VariableAccessor Vars { get; }

        /// <summary>
        /// Initializes a new instance of the CodeScriptContext
        /// </summary>
        /// <param name="connectionManager">Database connection manager</param>
        /// <param name="variables">Variables for script generation (optional)</param>
        public CodeScriptContext(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables = null)
        {
            ConnectionManager = connectionManager ?? throw new System.ArgumentNullException(nameof(connectionManager));
            Variables = variables ?? new Dictionary<string, string>();
            Vars = new VariableAccessor(Variables);
        }
    }
}
