using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Services
{
    /// <summary>
    /// Handles preview run execution of migration scripts (preview without actual execution)
    /// </summary>
    public class RunPreviewExecutionService
    {
        private readonly DbReactorConfiguration _configuration;
        private readonly MigrationFilteringService _filteringService;

        public RunPreviewExecutionService(
            DbReactorConfiguration configuration,
            MigrationFilteringService filteringService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _filteringService = filteringService ?? throw new ArgumentNullException(nameof(filteringService));
        }

        /// <summary>
        /// Performs a dry run of migrations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>DbReactorPreviewResult showing what would be executed</returns>
        public async Task<DbReactorPreviewResult> RunPreviewAsync(CancellationToken cancellationToken = default)
        {
            DbReactorPreviewResult result = new DbReactorPreviewResult();

            try
            {
                _configuration.LogProvider?.WriteInformation("Starting Run Preview analysis...");

                IEnumerable<IMigration> allMigrations = await _filteringService.GetAllMigrationsAsync(cancellationToken);
                bool databaseExists = await CheckDatabaseExistsAsync(cancellationToken);

                if (!databaseExists)
                {
                    await HandleNonExistentDatabase(result, allMigrations);
                }
                else
                {
                    await HandleExistingDatabase(result, allMigrations, cancellationToken);
                }

                LogAnalysisComplete(result);
            }
            catch (Exception ex)
            {
                _configuration.LogProvider?.WriteError($"Run Preview analysis failed: {ex.Message}");
            }

            return result;
        }

        private async Task HandleNonExistentDatabase(DbReactorPreviewResult result, IEnumerable<IMigration> allMigrations)
        {
            if (_configuration.CreateDatabaseIfNotExists)
            {
                _configuration.LogProvider?.WriteInformation("Database does not exist - would be created. All migrations would be executed.");
                AddUpgradeMigrations(result, allMigrations, executed: false);
            }
            else
            {
                _configuration.LogProvider?.WriteError("Database does not exist and CreateDatabaseIfNotExists is disabled");
            }
        }

        private async Task HandleExistingDatabase(DbReactorPreviewResult result, IEnumerable<IMigration> allMigrations, CancellationToken cancellationToken)
        {
            try
            {
                IEnumerable<IMigration> executedMigrations = await _filteringService.GetAppliedUpgradesAsync(cancellationToken);
                HashSet<string> executedMigrationNames = new HashSet<string>(executedMigrations.Select(m => m.Name));

                AddUpgradeMigrationsWithExecutionStatus(result, allMigrations, executedMigrationNames);
                await AddDowngradeMigrations(result, cancellationToken);
            }
            catch (Exception ex)
            {
                _configuration.LogProvider?.WriteWarning($"Could not access migration journal: {ex.Message}. Assuming no migrations have been executed.");
                AddUpgradeMigrations(result, allMigrations, executed: false);
            }
        }

        private void AddUpgradeMigrations(DbReactorPreviewResult result, IEnumerable<IMigration> migrations, bool executed)
        {
            foreach (IMigration migration in migrations)
            {
                string action = executed ? "already executed (skipped)" : "Would execute";
                _configuration.LogProvider?.WriteInformation($"Upgrade migration {action}: {migration.Name}");

                result.MigrationResults.Add(CreateUpgradeResult(migration, executed));
            }
        }

        private void AddUpgradeMigrationsWithExecutionStatus(DbReactorPreviewResult result, IEnumerable<IMigration> allMigrations, HashSet<string> executedMigrationNames)
        {
            foreach (IMigration migration in allMigrations)
            {
                bool alreadyExecuted = executedMigrationNames.Contains(migration.Name);
                string action = alreadyExecuted ? "already executed (skipped)" : "Would execute";
                _configuration.LogProvider?.WriteInformation($"Upgrade migration {action}: {migration.Name}");

                result.MigrationResults.Add(CreateUpgradeResult(migration, alreadyExecuted));
            }
        }

        private async Task AddDowngradeMigrations(DbReactorPreviewResult result, CancellationToken cancellationToken)
        {
            IEnumerable<MigrationJournalEntry> entriesToDowngrade = await _filteringService.GetEntriesToDowngradeAsync(cancellationToken);
            foreach (MigrationJournalEntry entry in entriesToDowngrade)
            {
                _configuration.LogProvider?.WriteInformation($"Would execute downgrade migration: {entry.MigrationName}");
                result.MigrationResults.Add(CreateDowngradeResult(entry));
            }
        }

        private static RunPreviewResult CreateUpgradeResult(IMigration migration, bool alreadyExecuted)
        {
            return new RunPreviewResult
            {
                Migration = migration,
                MigrationName = migration.Name,
                AlreadyExecuted = alreadyExecuted,
                IsUpgrade = true
            };
        }

        private static RunPreviewResult CreateDowngradeResult(MigrationJournalEntry entry)
        {
            return new RunPreviewResult
            {
                Migration = null,
                MigrationName = entry.MigrationName,
                AlreadyExecuted = false,
                IsUpgrade = false
            };
        }

        private void LogAnalysisComplete(DbReactorPreviewResult result)
        {
            _configuration.LogProvider?.WriteInformation(
                $"Run Preview analysis complete. Total: {result.TotalMigrations}, " +
                $"Pending: {result.PendingMigrations} (Upgrades: {result.PendingUpgrades}, Downgrades: {result.PendingDowngrades}), " +
                $"Already executed: {result.SkippedMigrations}");
        }

        /// <summary>
        /// Safely checks if the database exists
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if database exists, false otherwise</returns>
        private async Task<bool> CheckDatabaseExistsAsync(CancellationToken cancellationToken = default)
        {
            if (_configuration.DatabaseProvisioner == null)
            {
                _configuration.LogProvider?.WriteWarning("Database provisioner not configured. Cannot check database existence.");
                return false;
            }

            try
            {
                return await _configuration.DatabaseProvisioner.DatabaseExistsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _configuration.LogProvider?.WriteError($"Error checking database existence: {ex.Message}");
                return false;
            }
        }

    }
}