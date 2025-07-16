using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models.Contexts;
using DbReactor.Core.Utilities;
using System;
using System.Collections.Generic;

namespace DbReactor.Core.Models.Scripts
{
    /// <summary>
    /// Wraps a code script to implement IScript interface for integration with existing migration system
    /// </summary>
    public class ManagedCodeScript : IScript
    {
        private readonly ICodeScript _codeScript;
        private readonly string _hash;
        private string _generatedScript;

        public ManagedCodeScript(ICodeScript codeScript)
        {
            _codeScript = codeScript ?? throw new ArgumentNullException(nameof(codeScript));
            _hash = HashUtility.GenerateCodeScriptHash(_codeScript.GetType().FullName);
        }

        public string Name
        {
            get
            {
                // Use the full type name, replacing '+' (for nested types) with '.'
                // This ensures proper sorting with other script types
                string typeName = _codeScript.GetType().FullName.Replace('+', '.');
                return $"{typeName}";
            }
        }

        public string Script => _generatedScript ?? $"-- Code script: {_codeScript.GetType().FullName}";

        public string Hash => _hash;

        public ICodeScript CodeScript => _codeScript;

        /// <summary>
        /// Generates the upgrade script using the connection manager
        /// </summary>
        public void GenerateUpgradeScript(IConnectionManager connectionManager)
        {
            var context = new CodeScriptContext(connectionManager);
            _generatedScript = _codeScript.GetUpgradeScript(context);
        }

        /// <summary>
        /// Generates the upgrade script using the connection manager with variables
        /// </summary>
        public void GenerateUpgradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables)
        {
            var context = new CodeScriptContext(connectionManager, variables);
            _generatedScript = _codeScript.GetUpgradeScript(context);
        }

        /// <summary>
        /// Generates the downgrade script using the connection manager
        /// </summary>
        public string GenerateDowngradeScript(IConnectionManager connectionManager)
        {
            var context = new CodeScriptContext(connectionManager);
            return _codeScript.GetDowngradeScript(context);
        }

        /// <summary>
        /// Generates the downgrade script using the connection manager with variables
        /// </summary>
        public string GenerateDowngradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables)
        {
            var context = new CodeScriptContext(connectionManager, variables);
            return _codeScript.GetDowngradeScript(context);
        }

    }
}