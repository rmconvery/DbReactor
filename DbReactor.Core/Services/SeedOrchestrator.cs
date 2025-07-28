using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Constants;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using DbReactor.Core.Models.Scripts;
using DbReactor.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Services
{
    /// <summary>
    /// Orchestrates the seed execution process
    /// </summary>
    public class SeedOrchestrator
    {
        private readonly DbReactorConfiguration _configuration;
        private readonly SeedDiscoveryService _discoveryService;
        private readonly ISeedJournal _seedJournal;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly VariableSubstitutionService _variableService;

        public SeedOrchestrator(
            DbReactorConfiguration configuration,
            SeedDiscoveryService discoveryService,
            ISeedJournal seedJournal,
            IScriptExecutor scriptExecutor,
            VariableSubstitutionService variableService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
            _seedJournal = seedJournal ?? throw new ArgumentNullException(nameof(seedJournal));
            _scriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
        }

        /// <summary>
        /// Executes all applicable seeds
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of seed execution</returns>
        public async Task<DbReactorResult> ExecuteSeedsAsync(CancellationToken cancellationToken = default)
        {
            var result = new DbReactorResult();

            try
            {
                _configuration.LogProvider?.WriteInformation("Starting seed execution...");

                // Ensure seed journal table exists
                await _seedJournal.EnsureTableExistsAsync(_configuration.ConnectionManager, cancellationToken);

                // Get all seeds
                var seeds = await _discoveryService.GetSeedsAsync(cancellationToken);

                if (!seeds.Any())
                {
                    _configuration.LogProvider?.WriteInformation("No seeds found.");
                    result.Successful = true;
                    return result;
                }

                // Filter seeds that should execute
                var seedsToExecute = new List<ISeed>();
                foreach (var seed in seeds)
                {
                    var shouldExecute = await seed.Strategy.ShouldExecuteAsync(seed, _seedJournal, cancellationToken);
                    if (shouldExecute)
                    {
                        seedsToExecute.Add(seed);
                    }
                }

                if (!seedsToExecute.Any())
                {
                    _configuration.LogProvider?.WriteInformation("No seeds need to be executed.");
                    result.Successful = true;
                    return result;
                }

                _configuration.LogProvider?.WriteInformation($"Found {seedsToExecute.Count} seed(s) to execute.");

                // Execute each seed
                foreach (var seed in seedsToExecute)
                {
                    var seedResult = await ExecuteSeedAsync(seed, cancellationToken);
                    result.Scripts.Add(seedResult);

                    if (!seedResult.Successful)
                    {
                        result.Successful = false;
                        result.Error = seedResult.Error;
                        result.ErrorMessage = $"Failed to execute seed: {seed.Name}. {seedResult.ErrorMessage}";
                        _configuration.LogProvider?.WriteError(result.ErrorMessage);
                        break;
                    }

                    _configuration.LogProvider?.WriteInformation($"Successfully executed seed: {seed.Name}");
                }

                result.Successful = result.Scripts.All(s => s.Successful);
                _configuration.LogProvider?.WriteInformation($"Seed execution completed. Success: {result.Successful}");
            }
            catch (Exception ex)
            {
                result.Successful = false;
                result.Error = ex;
                result.ErrorMessage = $"Seed execution failed: {ex.Message}";
                _configuration.LogProvider?.WriteError(result.ErrorMessage);
            }

            return result;
        }

        /// <summary>
        /// Previews which seeds would be executed without actually executing them
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Preview result showing which seeds would execute</returns>
        public async Task<DbReactorSeedPreviewResult> PreviewSeedsAsync(CancellationToken cancellationToken = default)
        {
            var result = new DbReactorSeedPreviewResult();

            try
            {
                _configuration.LogProvider?.WriteInformation("Starting seed preview...");

                // Ensure seed journal table exists
                await _seedJournal.EnsureTableExistsAsync(_configuration.ConnectionManager, cancellationToken);

                // Get all seeds
                var seeds = await _discoveryService.GetSeedsAsync(cancellationToken);

                if (!seeds.Any())
                {
                    _configuration.LogProvider?.WriteInformation("No seeds found for preview.");
                    return result;
                }

                // Analyze each seed
                foreach (var seed in seeds)
                {
                    var shouldExecute = await seed.Strategy.ShouldExecuteAsync(seed, _seedJournal, cancellationToken);
                    var reason = await GetExecutionReasonAsync(seed, shouldExecute, cancellationToken);

                    var previewResult = new SeedPreviewResult
                    {
                        Seed = seed,
                        SeedName = seed.Name,
                        Strategy = seed.Strategy.Name,
                        WouldExecute = shouldExecute,
                        ExecutionReason = reason
                    };

                    result.SeedResults.Add(previewResult);
                }

                _configuration.LogProvider?.WriteInformation($"Seed preview completed. {result.Summary}");
            }
            catch (Exception ex)
            {
                _configuration.LogProvider?.WriteError($"Seed preview failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets the reason why a seed would or would not execute
        /// </summary>
        /// <param name="seed">The seed to analyze</param>
        /// <param name="wouldExecute">Whether the seed would execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Human-readable reason</returns>
        private async Task<string> GetExecutionReasonAsync(ISeed seed, bool wouldExecute, CancellationToken cancellationToken)
        {
            var strategyName = seed.Strategy.Name;

            if (!wouldExecute)
            {
                switch (strategyName)
                {
                    case DbReactorConstants.SeedStrategies.RunOnce:
                        return DbReactorConstants.SeedExecutionReasons.AlreadyExecutedRunOnce;
                    case DbReactorConstants.SeedStrategies.RunIfChanged:
                        return DbReactorConstants.SeedExecutionReasons.ContentNotChangedRunIfChanged;
                    default:
                        return string.Format(DbReactorConstants.SeedExecutionReasons.StrategyDeterminedNotToExecute, strategyName);
                }
            }

            switch (strategyName)
            {
                case DbReactorConstants.SeedStrategies.RunAlways:
                    return DbReactorConstants.SeedExecutionReasons.WillExecuteEveryTimeRunAlways;
                case DbReactorConstants.SeedStrategies.RunOnce:
                    return DbReactorConstants.SeedExecutionReasons.NotYetExecutedRunOnce;
                case DbReactorConstants.SeedStrategies.RunIfChanged:
                    return DbReactorConstants.SeedExecutionReasons.ContentHasChangedRunIfChanged;
                default:
                    return string.Format(DbReactorConstants.SeedExecutionReasons.StrategyDeterminedToExecute, strategyName);
            }
        }

        /// <summary>
        /// Executes a single seed
        /// </summary>
        /// <param name="seed">The seed to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of seed execution</returns>
        private async Task<MigrationResult> ExecuteSeedAsync(ISeed seed, CancellationToken cancellationToken)
        {
            try
            {
                // Perform variable substitution
                var scriptContent = _variableService.SubstituteVariables(seed.Script.Script, _configuration.Variables);

                // Create a temporary script with substituted content
                var substitutedScript = new GenericScript(seed.Script.Name, scriptContent);

                // Execute the script
                var result = await _scriptExecutor.ExecuteAsync(substitutedScript, _configuration.ConnectionManager, cancellationToken);

                if (result.Successful)
                {
                    // Record execution in journal
                    await _seedJournal.RecordExecutionAsync(seed, DateTime.UtcNow, cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new MigrationResult
                {
                    Successful = false,
                    Error = ex,
                    ErrorMessage = ex.Message,
                    Script = seed.Script
                };
            }
        }
    }
}