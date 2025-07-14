using DbReactor.Core.Abstractions;
using DbReactor.Core.Discovery;
using DbReactor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbReactor.Core.Implementations.Discovery
{
    /// <summary>
    /// Builds migrations by combining upgrade scripts with their corresponding downgrade scripts
    /// </summary>
    public class MigrationBuilder : IMigrationBuilder
    {
        private readonly IEnumerable<IScriptProvider> _scriptProviders;
        private readonly IDowngradeResolver _downgradeResolver;

        public MigrationBuilder(IScriptProvider scriptProvider, IDowngradeResolver downgradeResolver = null)
        {
            if (scriptProvider == null) throw new ArgumentNullException(nameof(scriptProvider));
            _scriptProviders = new[] { scriptProvider };
            _downgradeResolver = downgradeResolver;
        }

        public MigrationBuilder(IEnumerable<IScriptProvider> scriptProviders, IDowngradeResolver downgradeResolver = null)
        {
            _scriptProviders = scriptProviders ?? throw new ArgumentNullException(nameof(scriptProviders));
            _downgradeResolver = downgradeResolver;
        }

        public IEnumerable<IMigration> BuildMigrations()
        {
            List<IScript> allScripts = new List<IScript>();

            // Collect scripts from all providers
            foreach (IScriptProvider provider in _scriptProviders)
            {
                allScripts.AddRange(provider.GetScripts());
            }

            // Sort by name to ensure proper execution order (001_a.sql, 002_b.cs, 003_c.sql)
            IOrderedEnumerable<IScript> sortedScripts = allScripts.OrderBy(s => s.Name);

            foreach (IScript upgradeScript in sortedScripts)
            {
                IScript downgradeScript = _downgradeResolver?.FindDowngradeFor(upgradeScript);

                // Extract base name from script name (remove file extension)
                string baseName = GetBaseName(upgradeScript.Name);

                yield return new Migration(
                    name: baseName,
                    upgradeScript: upgradeScript,
                    downgradeScript: downgradeScript
                );
            }
        }

        private string GetBaseName(string scriptName)
        {
            // Remove common file extensions
            string[] extensions = new[] { ".sql", ".SQL", ".cs", ".vb", ".fs" };

            foreach (string ext in extensions)
            {
                if (scriptName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return scriptName.Substring(0, scriptName.Length - ext.Length);
                }
            }

            return scriptName;
        }
    }
}