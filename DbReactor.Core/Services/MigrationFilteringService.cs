using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Constants;
using DbReactor.Core.Enumerations;
using DbReactor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Services
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

        public async Task<bool> HasPendingUpgradesAsync(CancellationToken cancellationToken = default)
        {
            var pendingUpgrades = await GetPendingUpgradesAsync(cancellationToken);
            return pendingUpgrades.Any();
        }

        public async Task<IEnumerable<IMigration>> GetPendingUpgradesAsync(CancellationToken cancellationToken = default)
        {
            var migrations = GetMigrations();
            var pendingMigrations = new List<IMigration>();
            
            foreach (var migration in migrations)
            {
                var hasBeenExecuted = await _configuration.MigrationJournal.HasBeenExecutedAsync(migration, cancellationToken);
                if (!hasBeenExecuted)
                {
                    pendingMigrations.Add(migration);
                }
            }
            
            return pendingMigrations;
        }

        public async Task<IEnumerable<IMigration>> GetAppliedUpgradesAsync(CancellationToken cancellationToken = default)
        {
            var migrations = GetMigrations();
            var appliedMigrations = new List<IMigration>();
            
            foreach (var migration in migrations)
            {
                var hasBeenExecuted = await _configuration.MigrationJournal.HasBeenExecutedAsync(migration, cancellationToken);
                if (hasBeenExecuted)
                {
                    appliedMigrations.Add(migration);
                }
            }
            
            return appliedMigrations;
        }

        public async Task<IEnumerable<MigrationJournalEntry>> GetEntriesToDowngradeAsync(CancellationToken cancellationToken = default)
        {
            // Get all executed scripts from the journal
            var executedMigrationJournalEntries = await _configuration.MigrationJournal.GetExecutedMigrationsAsync(cancellationToken);

            // Get migrations
            var migrations = GetMigrations();

            // Identify journal entries that are not in the upgrade scripts list
            return executedMigrationJournalEntries
                .Where(entry => !migrations.Any(migration => migration.UpgradeScript.Hash == entry.UpgradeScriptHash))
                .Reverse();
        }

        public async Task<IEnumerable<IMigration>> GetAllMigrationsAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(GetMigrations());
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
            string baseName = scriptName;

            // Remove common file extensions
            foreach (string ext in DbReactorConstants.FileExtensions.All)
            {
                if (baseName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    baseName = baseName.Substring(0, baseName.Length - ext.Length);
                    break; // Only remove one extension
                }
            }

            // Remove leading underscores for ordering
            return baseName.TrimStart('_');
        }
    }
}