using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Constants;
using DbReactor.Core.Exceptions;
using DbReactor.Core.Models;
using DbReactor.Core.Models.Scripts;
using DbReactor.Core.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Engine
{
    /// <summary>
    /// Handles the execution of individual migration scripts
    /// </summary>
    public class ScriptExecutionService
    {
        private readonly DbReactorConfiguration _configuration;
        private readonly ITimeProvider _timeProvider;
        private readonly VariableSubstitutionService _variableService;

        public ScriptExecutionService(DbReactorConfiguration configuration, ITimeProvider timeProvider = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _timeProvider = timeProvider ?? new SystemTimeProvider();
            _variableService = new VariableSubstitutionService();
        }

        public async Task<MigrationResult> ExecuteUpgradeAsync(IMigration migration, CancellationToken cancellationToken = default)
        {
            DateTime startTime = _timeProvider.UtcNow;
            MigrationResult result = new MigrationResult { Script = migration.UpgradeScript };

            try
            {
                // Prepare the script (generate if needed)
                string downgradeScript = PrepareUpgradeScript(migration);

                // Get script content and apply variable substitution for SQL scripts only
                // Code scripts already handle variables internally
                string scriptContent = migration.UpgradeScript.Script;
                IScript executableScript = migration.UpgradeScript;

                if (_configuration.EnableVariables && _configuration.Variables?.Count > 0 && !(migration.UpgradeScript is ManagedCodeScript))
                {
                    // Only apply string substitution to SQL/embedded scripts, not code scripts
                    scriptContent = _variableService.SubstituteVariables(scriptContent, _configuration.Variables);
                    executableScript = new GenericScript(migration.UpgradeScript.Name, scriptContent);
                }

                if (string.IsNullOrWhiteSpace(scriptContent))
                {
                    throw new MigrationExecutionException(DbReactorConstants.ErrorMessages.UpgradeScriptContentEmpty, migration.Name);
                }

                // Execute the script
                try
                {
                    result = await _configuration.ScriptExecutor.ExecuteAsync(executableScript, _configuration.ConnectionManager, cancellationToken);
                    result.ExecutionTime = _timeProvider.UtcNow - startTime;
                }
                catch (Exception ex)
                {
                    throw new MigrationExecutionException(
                        string.Format(DbReactorConstants.ErrorMessages.MigrationExecutionFailed, migration.Name, ex.Message),
                        migration.Name,
                        ex);
                }

                if (result.Successful)
                {
                    // Store in journal with downgrade script
                    Migration migrationWithDowngrade = new Migration(
                        migration.Name,
                        migration.UpgradeScript,
                        migration.DowngradeScript,
                        downgradeScript
                    );
                    await _configuration.MigrationJournal.StoreExecutedMigrationAsync(migrationWithDowngrade, result, cancellationToken);
                }
            }
            catch (MigrationExecutionException)
            {
                // Re-throw our custom exceptions
                throw;
            }
            catch (Exception ex)
            {
                result.Successful = false;
                result.Error = ex;
                result.ErrorMessage = ex.Message;
                result.ExecutionTime = _timeProvider.UtcNow - startTime;

                throw new MigrationExecutionException(
                    string.Format(DbReactorConstants.ErrorMessages.MigrationExecutionFailed, migration.Name, ex.Message),
                    migration.Name,
                    ex);
            }

            return result;
        }

        public async Task<MigrationResult> ExecuteDowngradeAsync(MigrationJournalEntry entry, CancellationToken cancellationToken = default)
        {
            DateTime startTime = _timeProvider.UtcNow;
            MigrationResult result = new MigrationResult();

            try
            {
                if (string.IsNullOrWhiteSpace(entry.DowngradeScript))
                {
                    throw new MigrationExecutionException(string.Format(DbReactorConstants.ErrorMessages.MigrationDoesNotSupportDowngrade, entry.MigrationName), entry.MigrationName);
                }

                // Apply variable substitution to downgrade script if enabled
                string scriptContent = entry.DowngradeScript;
                if (_configuration.EnableVariables && _configuration.Variables?.Count > 0)
                {
                    scriptContent = _variableService.SubstituteVariables(scriptContent, _configuration.Variables);
                }

                // Create a Script instance for downgrade execution
                IScript script = new GenericScript(
                    name: entry.MigrationName,
                    script: scriptContent
                );

                result.Script = script;

                // Validate script content
                if (string.IsNullOrWhiteSpace(scriptContent))
                {
                    throw new MigrationExecutionException(DbReactorConstants.ErrorMessages.DowngradeScriptContentEmpty, entry.MigrationName);
                }

                // Execute the script
                try
                {
                    result = await _configuration.ScriptExecutor.ExecuteAsync(script, _configuration.ConnectionManager, cancellationToken);
                    result.ExecutionTime = _timeProvider.UtcNow - startTime;
                }
                catch (Exception ex)
                {
                    throw new MigrationExecutionException(
                        string.Format(DbReactorConstants.ErrorMessages.DowngradeExecutionFailed, entry.MigrationName, ex.Message),
                        entry.MigrationName,
                        ex);
                }

                if (result.Successful)
                {
                    // Remove from journal
                    await _configuration.MigrationJournal.RemoveExecutedMigrationAsync(entry.UpgradeScriptHash, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                result.Successful = false;
                result.Error = ex;
                result.ErrorMessage = ex.Message;
                result.ExecutionTime = _timeProvider.UtcNow - startTime;
            }

            return result;
        }

        private string PrepareUpgradeScript(IMigration migration)
        {
            string downgradeScript = null;

            if (migration.UpgradeScript is ManagedCodeScript codeScriptWrapper)
            {
                // Always generate the upgrade script first
                if (_configuration.EnableVariables && _configuration.Variables?.Count > 0)
                {
                    // Use variables if enabled
                    codeScriptWrapper.GenerateUpgradeScript(_configuration.ConnectionManager, _configuration.Variables);

                    // Use the code script's own downgrade if supported
                    if (codeScriptWrapper.CodeScript.SupportsDowngrade)
                    {
                        downgradeScript = codeScriptWrapper.GenerateDowngradeScript(_configuration.ConnectionManager, _configuration.Variables);
                    }
                }
                else
                {
                    // Fallback to non-variable version
                    codeScriptWrapper.GenerateUpgradeScript(_configuration.ConnectionManager);

                    // Use the code script's own downgrade if supported
                    if (codeScriptWrapper.CodeScript.SupportsDowngrade)
                    {
                        downgradeScript = codeScriptWrapper.GenerateDowngradeScript(_configuration.ConnectionManager);
                    }
                }
            }
            else if (migration.DowngradeScript != null)
            {
                // For SQL/embedded scripts, use the explicit downgrade if present
                downgradeScript = migration.DowngradeScript.Script;

                // Apply variable substitution to downgrade script if enabled
                if (_configuration.EnableVariables && _configuration.Variables?.Count > 0)
                {
                    downgradeScript = _variableService.SubstituteVariables(downgradeScript, _configuration.Variables);
                }
            }

            return downgradeScript;
        }
    }

    /// <summary>
    /// Interface for providing current time (for testability)
    /// </summary>
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }

    /// <summary>
    /// System implementation of time provider
    /// </summary>
    public class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}