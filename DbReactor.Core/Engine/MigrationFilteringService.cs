using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Constants;
using DbReactor.Core.Enumerations;
using DbReactor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbReactor.Core.Engine
{
    /// <summary>
    /// Handles filtering and ordering of migrations
    /// </summary>
    public class MigrationFilteringService
    {
        private readonly DbReactorConfiguration _configuration;

        public MigrationFilteringService(DbReactorConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public bool HasPendingUpgrades()
        {
            return GetPendingUpgrades().Any();
        }

        public IEnumerable<IMigration> GetPendingUpgrades()
        {
            IEnumerable<IMigration> migrations = GetMigrations();
            return migrations.Where(migration => !_configuration.MigrationJournal.HasBeenExecuted(migration));
        }

        public IEnumerable<IMigration> GetAppliedUpgrades()
        {
            IEnumerable<IMigration> migrations = GetMigrations();
            return migrations.Where(migration => _configuration.MigrationJournal.HasBeenExecuted(migration));
        }

        public IEnumerable<MigrationJournalEntry> GetEntriesToDowngrade()
        {
            // Get all executed scripts from the journal
            IEnumerable<MigrationJournalEntry> executedMigrationJournalEntries = _configuration.MigrationJournal.GetExecutedMigrations();

            // Get migrations
            IEnumerable<IMigration> migrations = GetMigrations();

            // Identify journal entries that are not in the upgrade scripts list
            return executedMigrationJournalEntries
                .Where(entry => !migrations.Any(migration => migration.UpgradeScript.Hash == entry.UpgradeScriptHash))
                .Reverse();
        }

        private IEnumerable<IMigration> GetMigrations()
        {
            IEnumerable<IMigration> migrations;

            if (_configuration.MigrationBuilder != null)
            {
                migrations = _configuration.MigrationBuilder.BuildMigrations();
            }
            else
            {
                var allScripts = _configuration.ScriptProviders
                    .SelectMany(provider => provider.GetScripts());
                    
                migrations = allScripts
                    .Select(script => new Migration(
                        name: GetBaseName(script.Name),
                        upgradeScript: script,
                        downgradeScript: null
                    ));
            }

            return ApplyOrdering(migrations);
        }

        private IEnumerable<IMigration> ApplyOrdering(IEnumerable<IMigration> migrations)
        {
            switch (_configuration.ExecutionOrder)
            {
                case ScriptExecutionOrder.ByNameAscending:
                    return migrations.OrderBy(m => GetBaseName(m.Name));
                case ScriptExecutionOrder.ByNameDescending:
                    return migrations.OrderByDescending(m => GetBaseName(m.Name));
                default:
                    return migrations;
            }
        }

        private string GetBaseName(string scriptName)
        {
            // Get the last segment after the last dot
            int lastDot = scriptName.LastIndexOf('.');
            string baseName = lastDot >= 0 ? scriptName.Substring(lastDot + 1) : scriptName;

            // Remove common file extensions
            foreach (string ext in DbReactorConstants.FileExtensions.All)
            {
                if (baseName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    baseName = baseName.Substring(0, baseName.Length - ext.Length);
                }
            }

            // Remove leading underscores for ordering
            return baseName.TrimStart('_');
        }
    }
}