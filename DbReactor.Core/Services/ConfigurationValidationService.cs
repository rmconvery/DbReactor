using DbReactor.Core.Configuration;
using DbReactor.Core.Constants;
using DbReactor.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbReactor.Core.Services
{
    /// <summary>
    /// Validates DbReactor configuration for correctness and completeness
    /// </summary>
    public class ConfigurationValidationService
    {
        /// <summary>
        /// Validates the configuration and returns any validation errors
        /// </summary>
        /// <param name="configuration">The configuration to validate</param>
        /// <returns>List of validation error messages</returns>
        public List<string> ValidateConfiguration(DbReactorConfiguration configuration)
        {
            List<string> errors = new List<string>();

            if (configuration == null)
            {
                errors.Add("Configuration cannot be null");
                return errors;
            }

            // Required components
            if (configuration.ConnectionManager == null)
                errors.Add(DbReactorConstants.ErrorMessages.ConnectionManagerRequired);

            if (!configuration.ScriptProviders.Any())
                errors.Add(DbReactorConstants.ErrorMessages.ScriptProviderRequired);

            if (configuration.MigrationJournal == null)
                errors.Add(DbReactorConstants.ErrorMessages.ScriptJournalRequired);

            if (configuration.ScriptExecutor == null)
                errors.Add(DbReactorConstants.ErrorMessages.ScriptExecutorRequired);

            // Conditional validations
            if (configuration.CreateDatabaseIfNotExists && configuration.DatabaseProvisioner == null)
                errors.Add(DbReactorConstants.ErrorMessages.DatabaseProvisionerRequired);

            // Downgrade configuration validation
            if (configuration.AllowDowngrades && configuration.DowngradeResolver == null)
                errors.Add(DbReactorConstants.ErrorMessages.DowngradeResolverRequired);

            return errors;
        }

        /// <summary>
        /// Validates configuration and throws an exception if invalid
        /// </summary>
        /// <param name="configuration">The configuration to validate</param>
        /// <exception cref="ConfigurationException">Thrown when configuration is invalid</exception>
        public void ValidateAndThrow(DbReactorConfiguration configuration)
        {
            List<string> errors = ValidateConfiguration(configuration);
            if (errors.Any())
            {
                string errorMessage = string.Format(DbReactorConstants.ErrorMessages.ConfigurationValidationFailed, string.Join("\n", errors));
                throw new ConfigurationException(errorMessage);
            }
        }
    }
}