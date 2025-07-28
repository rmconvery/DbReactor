using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using DbReactor.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Engine
{
    /// <summary>
    /// Main implementation of the database reactor engine
    /// </summary>
    public class DbReactorEngine : IDbReactorEngine
    {
        private readonly MigrationOrchestrator _orchestrator;
        private readonly MigrationFilteringService _filteringService;
        private readonly SeedOrchestrator _seedOrchestrator;
        private readonly DbReactorConfiguration _configuration;

        public DbReactorEngine(DbReactorConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _configuration = configuration;

            // Validate configuration
            ConfigurationValidationService validationService = new ConfigurationValidationService();
            validationService.ValidateAndThrow(configuration);

            // Initialize services
            ScriptExecutionService executionService = new ScriptExecutionService(configuration);
            _filteringService = new MigrationFilteringService(configuration);
            _orchestrator = new MigrationOrchestrator(configuration, executionService, _filteringService);

            // Initialize seeding services if enabled
            if (configuration.EnableSeeding && configuration.SeedJournal != null)
            {
                var seedDiscoveryService = new SeedDiscoveryService(
                    configuration.SeedScriptProviders,
                    configuration.SeedStrategyResolvers,
                    configuration.GlobalSeedStrategy,
                    configuration.FallbackSeedStrategy);

                var variableService = new VariableSubstitutionService();

                _seedOrchestrator = new SeedOrchestrator(
                    configuration,
                    seedDiscoveryService,
                    configuration.SeedJournal,
                    configuration.ScriptExecutor,
                    variableService);
            }
        }

        public async Task<DbReactorResult> RunAsync(CancellationToken cancellationToken = default)
        {
            return await _orchestrator.ExecuteMigrationsAsync(cancellationToken);
        }

        public async Task<DbReactorResult> ApplyUpgradesAsync(CancellationToken cancellationToken = default)
        {
            return await _orchestrator.ApplyUpgradesAsync(cancellationToken);
        }

        public async Task<DbReactorResult> ApplyDowngradesAsync(CancellationToken cancellationToken = default)
        {
            return await _orchestrator.ApplyDowngradesAsync(cancellationToken);
        }

        public async Task<DbReactorResult> ApplyLastDowngradeAsync(CancellationToken cancellationToken = default)
        {
            return await _orchestrator.ApplyLastDowngradeAsync(cancellationToken);
        }

        public async Task<bool> HasPendingUpgradesAsync(CancellationToken cancellationToken = default)
        {
            return await _filteringService.HasPendingUpgradesAsync(cancellationToken);
        }

        public async Task<IEnumerable<IMigration>> GetPendingUpgradesAsync(CancellationToken cancellationToken = default)
        {
            return await _filteringService.GetPendingUpgradesAsync(cancellationToken);
        }

        public async Task<IEnumerable<IMigration>> GetAppliedUpgradesAsync(CancellationToken cancellationToken = default)
        {
            return await _filteringService.GetAppliedUpgradesAsync(cancellationToken);
        }

        public async Task<DbReactorPreviewResult> RunPreviewAsync(CancellationToken cancellationToken = default)
        {
            return await _orchestrator.RunPreviewAsync(cancellationToken);
        }

        public async Task<DbReactorResult> ExecuteSeedsAsync(CancellationToken cancellationToken = default)
        {
            if (!_configuration.EnableSeeding || _seedOrchestrator == null)
            {
                return new DbReactorResult
                {
                    Successful = true,
                    ErrorMessage = "Seeding is not enabled or configured."
                };
            }

            return await _seedOrchestrator.ExecuteSeedsAsync(cancellationToken);
        }

        public async Task<DbReactorSeedPreviewResult> PreviewSeedsAsync(CancellationToken cancellationToken = default)
        {
            if (!_configuration.EnableSeeding || _seedOrchestrator == null)
            {
                return new DbReactorSeedPreviewResult();
            }

            return await _seedOrchestrator.PreviewSeedsAsync(cancellationToken);
        }
    }
}