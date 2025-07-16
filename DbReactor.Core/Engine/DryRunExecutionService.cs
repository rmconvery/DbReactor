using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Models;
using DbReactor.Core.Models.Scripts;
using DbReactor.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Engine
{
    /// <summary>
    /// Handles dry run execution of migration scripts (preview without actual execution)
    /// </summary>
    public class DryRunExecutionService
    {
        private readonly DbReactorConfiguration _configuration;
        private readonly VariableSubstitutionService _variableService;
        private readonly MigrationFilteringService _filteringService;

        public DryRunExecutionService(
            DbReactorConfiguration configuration,
            MigrationFilteringService filteringService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _filteringService = filteringService ?? throw new ArgumentNullException(nameof(filteringService));
            _variableService = new VariableSubstitutionService();
        }

        /// <summary>
        /// Performs a dry run of upgrade migrations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dry run result showing what would be executed</returns>
        public async Task<DbReactorDryRunResult> DryRunUpgradesAsync(CancellationToken cancellationToken = default)
        {
            var result = new DbReactorDryRunResult();

            try
            {
                _configuration.LogProvider?.WriteInformation("Starting dry run analysis...");

                // Get all migrations first
                var allMigrations = await _filteringService.GetAllMigrationsAsync(cancellationToken);

                // Check if database exists
                var databaseExists = await CheckDatabaseExistsAsync(cancellationToken);
                
                if (!databaseExists)
                {
                    if (_configuration.CreateDatabaseIfNotExists)
                    {
                        // Database doesn't exist but would be created - all migrations would run
                        _configuration.LogProvider?.WriteInformation("Database does not exist - would be created. All migrations would be executed.");
                        
                        foreach (var migration in allMigrations)
                        {
                            _configuration.LogProvider?.WriteInformation($"Would execute upgrade migration: {migration.Name}");
                            var migrationResult = new DryRunResult
                            {
                                Migration = migration,
                                MigrationName = migration.Name,
                                AlreadyExecuted = false, // None executed since database doesn't exist
                                IsUpgrade = true
                            };
                            result.MigrationResults.Add(migrationResult);
                        }
                    }
                    else
                    {
                        // Database doesn't exist and won't be created - show error
                        _configuration.LogProvider?.WriteError("Database does not exist and CreateDatabaseIfNotExists is disabled");
                        // Don't add any migration results since no migrations can be executed
                        return result;
                    }
                }
                else
                {
                    // Database exists - check migration journal
                    try
                    {
                        var executedMigrations = await _filteringService.GetAppliedUpgradesAsync(cancellationToken);
                        var executedMigrationNames = new HashSet<string>(executedMigrations.Select(m => m.Name));

                        // Check for upgrades
                        foreach (var migration in allMigrations)
                        {
                            bool alreadyExecuted = executedMigrationNames.Contains(migration.Name);
                            if (alreadyExecuted)
                            {
                                _configuration.LogProvider?.WriteInformation($"Upgrade migration already executed (skipped): {migration.Name}");
                            }
                            else
                            {
                                _configuration.LogProvider?.WriteInformation($"Would execute upgrade migration: {migration.Name}");
                            }
                            
                            var migrationResult = new DryRunResult
                            {
                                Migration = migration,
                                MigrationName = migration.Name,
                                AlreadyExecuted = alreadyExecuted,
                                IsUpgrade = true
                            };
                            result.MigrationResults.Add(migrationResult);
                        }

                        // Check for downgrades (migrations that were executed but are no longer in the upgrade scripts)
                        var entriesToDowngrade = await _filteringService.GetEntriesToDowngradeAsync(cancellationToken);
                        foreach (var entry in entriesToDowngrade)
                        {
                            _configuration.LogProvider?.WriteInformation($"Would execute downgrade migration: {entry.MigrationName}");
                            var migrationResult = new DryRunResult
                            {
                                Migration = null,
                                MigrationName = entry.MigrationName,
                                AlreadyExecuted = false,
                                IsUpgrade = false
                            };
                            result.MigrationResults.Add(migrationResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        // If we can't access the journal, assume no migrations have been executed
                        _configuration.LogProvider?.WriteWarning($"Could not access migration journal: {ex.Message}. Assuming no migrations have been executed.");
                        
                        foreach (var migration in allMigrations)
                        {
                            _configuration.LogProvider?.WriteInformation($"Would execute upgrade migration: {migration.Name}");
                            var migrationResult = new DryRunResult
                            {
                                Migration = migration,
                                MigrationName = migration.Name,
                                AlreadyExecuted = false,
                                IsUpgrade = true
                            };
                            result.MigrationResults.Add(migrationResult);
                        }
                    }
                }

                // Log summary
                _configuration.LogProvider?.WriteInformation($"Dry run analysis complete. Total: {result.TotalMigrations}, Pending: {result.PendingMigrations} (Upgrades: {result.PendingUpgrades}, Downgrades: {result.PendingDowngrades}), Already executed: {result.SkippedMigrations}");
            }
            catch (Exception ex)
            {
                _configuration.LogProvider?.WriteError($"Dry run analysis failed: {ex.Message}");
                
                // Don't add error results as migrations - they're already logged
            }

            return result;
        }

        /// <summary>
        /// Performs a dry run of downgrade migrations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dry run result showing what would be executed</returns>
        public async Task<DbReactorDryRunResult> DryRunDowngradesAsync(CancellationToken cancellationToken = default)
        {
            var result = new DbReactorDryRunResult();

            try
            {
                if (!_configuration.AllowDowngrades)
                {
                    result.MigrationResults.Add(new DryRunResult
                    {
                        Migration = null,
                        MigrationName = "Downgrade operations are disabled in configuration",
                        AlreadyExecuted = false
                    });
                    return result;
                }

                _configuration.LogProvider?.WriteInformation("Starting downgrade dry run analysis...");

                var entriesToDowngrade = await _filteringService.GetEntriesToDowngradeAsync(cancellationToken);

                foreach (var entry in entriesToDowngrade)
                {
                    _configuration.LogProvider?.WriteInformation($"Would execute downgrade migration: {entry.MigrationName}");
                    var migrationResult = new DryRunResult
                    {
                        Migration = null,
                        MigrationName = entry.MigrationName,
                        AlreadyExecuted = false,
                        IsUpgrade = false
                    };
                    result.MigrationResults.Add(migrationResult);
                }

                _configuration.LogProvider?.WriteInformation($"Downgrade dry run analysis complete. Total: {result.TotalMigrations}, Pending: {result.PendingMigrations} (Upgrades: {result.PendingUpgrades}, Downgrades: {result.PendingDowngrades}), Already executed: {result.SkippedMigrations}");
            }
            catch (Exception ex)
            {
                _configuration.LogProvider?.WriteError($"Downgrade dry run analysis failed: {ex.Message}");
                
                // Don't add error results as migrations - they're already logged
            }

            return result;
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