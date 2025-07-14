using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Models;
using System;
using System.Collections.Generic;

namespace DbReactor.Core.Engine
{
    /// <summary>
    /// Main implementation of the database reactor engine
    /// </summary>
    public class DbReactorEngine : IDbReactorEngine
    {
        private readonly MigrationOrchestrator _orchestrator;
        private readonly MigrationFilteringService _filteringService;

        public DbReactorEngine(DbReactorConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Validate configuration
            var validationService = new ConfigurationValidationService();
            validationService.ValidateAndThrow(configuration);

            // Initialize services
            var executionService = new ScriptExecutionService(configuration);
            _filteringService = new MigrationFilteringService(configuration);
            _orchestrator = new MigrationOrchestrator(configuration, executionService, _filteringService);
        }

        public DbReactorResult Run()
        {
            return _orchestrator.ExecuteMigrations();
        }

        public DbReactorResult ApplyUpgrades()
        {
            return _orchestrator.ApplyUpgrades();
        }

        public DbReactorResult ApplyDowngrades()
        {
            return _orchestrator.ApplyDowngrades();
        }

        public bool HasPendingUpgrades()
        {
            return _filteringService.HasPendingUpgrades();
        }

        public IEnumerable<IMigration> GetPendingUpgrades()
        {
            return _filteringService.GetPendingUpgrades();
        }

        public IEnumerable<IMigration> GetAppliedUpgrades()
        {
            return _filteringService.GetAppliedUpgrades();
        }
    }
}
