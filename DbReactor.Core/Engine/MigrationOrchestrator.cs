using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbReactor.Core.Engine
{
    /// <summary>
    /// Orchestrates the migration process, handling the high-level flow
    /// </summary>
    public class MigrationOrchestrator
    {
        private readonly DbReactorConfiguration _configuration;
        private readonly ScriptExecutionService _executionService;
        private readonly MigrationFilteringService _filteringService;

        public MigrationOrchestrator(
            DbReactorConfiguration configuration,
            ScriptExecutionService executionService,
            MigrationFilteringService filteringService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
            _filteringService = filteringService ?? throw new ArgumentNullException(nameof(filteringService));
        }

        public DbReactorResult ExecuteMigrations()
        {
            DbReactorResult result = new DbReactorResult();

            try
            {
                _configuration.LogProvider?.WriteInformation("Starting database reactor process...");

                // Apply upgrades
                _configuration.LogProvider?.WriteInformation("Applying upgrades...");
                DbReactorResult upgradeResult = ApplyUpgrades();
                result.Scripts.AddRange(upgradeResult.Scripts);

                if (!upgradeResult.Successful)
                {
                    result.Successful = false;
                    result.Error = upgradeResult.Error;
                    result.ErrorMessage = $"Upgrade process failed: {upgradeResult.ErrorMessage}";
                    _configuration.LogProvider?.WriteError(result.ErrorMessage);
                    return result;
                }

                // Apply downgrades if enabled
                if (_configuration.AllowDowngrades)
                {
                    _configuration.LogProvider?.WriteInformation("Applying downgrades...");
                    DbReactorResult downgradeResult = ApplyDowngrades();
                    result.Scripts.AddRange(downgradeResult.Scripts);

                    if (!downgradeResult.Successful)
                    {
                        result.Successful = false;
                        result.Error = downgradeResult.Error;
                        result.ErrorMessage = $"Downgrade process failed: {downgradeResult.ErrorMessage}";
                        _configuration.LogProvider?.WriteError(result.ErrorMessage);
                        return result;
                    }
                }

                result.Successful = true;
                _configuration.LogProvider?.WriteInformation("Database reactor process completed successfully.");
            }
            catch (Exception ex)
            {
                result.Successful = false;
                result.Error = ex;
                result.ErrorMessage = $"Database reactor process failed: {ex.Message}";
                _configuration.LogProvider?.WriteError(result.ErrorMessage);
            }

            return result;
        }

        public DbReactorResult ApplyUpgrades()
        {
            DbReactorResult result = new DbReactorResult();

            try
            {
                _configuration.LogProvider?.WriteInformation("Starting database migration...");

                // Ensure database exists if provisioner is configured
                if (_configuration.CreateDatabaseIfNotExists && _configuration.DatabaseProvisioner != null)
                {
                    _configuration.LogProvider?.WriteInformation("Ensuring database exists...");
                    _configuration.DatabaseProvisioner.EnsureDatabaseExists(_configuration.DatabaseCreationTemplate);
                }

                // Ensure journal table exists
                _configuration.MigrationJournal.EnsureTableExists(_configuration.ConnectionManager);

                // Get pending scripts
                IEnumerable<IMigration> pendingMigrations = _filteringService.GetPendingUpgrades();

                if (!pendingMigrations.Any())
                {
                    _configuration.LogProvider?.WriteInformation("No pending migrations found.");
                    result.Successful = true;
                    return result;
                }

                _configuration.LogProvider?.WriteInformation($"Found {pendingMigrations.Count()} pending migration(s).");

                // Execute each script
                foreach (IMigration migration in pendingMigrations)
                {
                    MigrationResult scriptResult = _executionService.ExecuteUpgrade(migration);
                    result.Scripts.Add(scriptResult);

                    if (!scriptResult.Successful)
                    {
                        result.Successful = false;
                        result.Error = scriptResult.Error;
                        result.ErrorMessage = $"Failed to execute script: {migration.Name}. {scriptResult.ErrorMessage}";
                        _configuration.LogProvider?.WriteError(result.ErrorMessage);
                        break;
                    }

                    _configuration.LogProvider?.WriteInformation($"Successfully executed script: {migration.Name}");
                }

                result.Successful = result.Scripts.All(s => s.Successful);
                _configuration.LogProvider?.WriteInformation($"Database migration completed. Success: {result.Successful}");
            }
            catch (Exception ex)
            {
                result.Successful = false;
                result.Error = ex;
                result.ErrorMessage = ex.Message;
                _configuration.LogProvider?.WriteError($"Database migration failed: {ex.Message}");
            }

            return result;
        }

        public DbReactorResult ApplyDowngrades()
        {
            DbReactorResult result = new DbReactorResult();

            try
            {
                // Ensure database exists if provisioner is configured
                if (_configuration.CreateDatabaseIfNotExists && _configuration.DatabaseProvisioner != null)
                {
                    _configuration.LogProvider?.WriteInformation("Ensuring database exists...");
                    _configuration.DatabaseProvisioner.EnsureDatabaseExists(_configuration.DatabaseCreationTemplate);
                }

                // Ensure journal table exists
                _configuration.MigrationJournal.EnsureTableExists(_configuration.ConnectionManager);

                // Check if downgrades are enabled
                if (!_configuration.AllowDowngrades)
                {
                    _configuration.LogProvider?.WriteInformation("Downgrade scripts are disabled in configuration. No changes will be reverted.");
                    result.Successful = true;
                    return result;
                }

                _configuration.LogProvider?.WriteInformation("Starting database downgrade...");

                // Get entries to downgrade
                IEnumerable<MigrationJournalEntry> entriesToDowngrade = _filteringService.GetEntriesToDowngrade();

                if (!entriesToDowngrade.Any())
                {
                    _configuration.LogProvider?.WriteInformation("No migration found for downgrade.");
                    result.Successful = true;
                    return result;
                }

                _configuration.LogProvider?.WriteInformation($"Found {entriesToDowngrade.Count()} migrations to downgrade.");

                // Execute downgrade for each journal entry
                foreach (MigrationJournalEntry entry in entriesToDowngrade)
                {
                    MigrationResult scriptResult = _executionService.ExecuteDowngrade(entry);
                    result.Scripts.Add(scriptResult);

                    if (!scriptResult.Successful)
                    {
                        result.Successful = false;
                        result.Error = scriptResult.Error;
                        result.ErrorMessage = $"Failed to revert script: {entry.MigrationName}. {scriptResult.ErrorMessage}";
                        _configuration.LogProvider?.WriteError(result.ErrorMessage);
                        break;
                    }

                    _configuration.LogProvider?.WriteInformation($"Successfully reverted script: {entry.MigrationName}");
                }

                result.Successful = result.Scripts.All(s => s.Successful);
                _configuration.LogProvider?.WriteInformation($"Database downgrade completed. Success: {result.Successful}");
            }
            catch (Exception ex)
            {
                result.Successful = false;
                result.Error = ex;
                result.ErrorMessage = ex.Message;
                _configuration.LogProvider?.WriteError($"Database downgrade failed: {ex.Message}");
            }

            return result;
        }
    }
}