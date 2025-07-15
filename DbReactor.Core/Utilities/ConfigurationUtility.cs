using DbReactor.Core.Configuration;
using DbReactor.Core.Implementations.Discovery;
using System;
using System.Linq;

namespace DbReactor.Core.Utilities
{
    /// <summary>
    /// Provides utilities for managing DbReactor configuration
    /// </summary>
    public static class ConfigurationUtility
    {
        /// <summary>
        /// Refreshes the migration builder with current script providers and downgrade resolver
        /// </summary>
        /// <param name="config">Configuration to refresh</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public static void RefreshMigrationBuilder(DbReactorConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            if (config.ScriptProviders?.Any() == true)
            {
                config.MigrationBuilder = new MigrationBuilder(config.ScriptProviders, config.DowngradeResolver);
            }
        }

        /// <summary>
        /// Validates that the configuration has the minimum required components
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public static void ValidateConfiguration(DbReactorConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            if (config.ScriptProviders == null || !config.ScriptProviders.Any())
            {
                throw new InvalidOperationException("Configuration must have at least one script provider configured.");
            }
            
            if (config.ConnectionManager == null)
            {
                throw new InvalidOperationException("Configuration must have a connection manager configured.");
            }
            
            if (config.MigrationJournal == null)
            {
                throw new InvalidOperationException("Configuration must have a migration journal configured.");
            }
            
            if (config.ScriptExecutor == null)
            {
                throw new InvalidOperationException("Configuration must have a script executor configured.");
            }
        }

        /// <summary>
        /// Ensures that the configuration has a migration builder, creating one if necessary
        /// </summary>
        /// <param name="config">Configuration to ensure has a migration builder</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public static void EnsureMigrationBuilder(DbReactorConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            if (config.MigrationBuilder == null)
            {
                RefreshMigrationBuilder(config);
            }
        }

        /// <summary>
        /// Checks if the configuration supports downgrade operations
        /// </summary>
        /// <param name="config">Configuration to check</param>
        /// <returns>True if downgrade operations are supported</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public static bool SupportsDowngrades(DbReactorConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            return config.AllowDowngrades && config.DowngradeResolver != null;
        }

        /// <summary>
        /// Gets the count of configured script providers
        /// </summary>
        /// <param name="config">Configuration to analyze</param>
        /// <returns>Number of script providers</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public static int GetScriptProviderCount(DbReactorConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            return config.ScriptProviders?.Count ?? 0;
        }

        /// <summary>
        /// Checks if the configuration has variables enabled
        /// </summary>
        /// <param name="config">Configuration to check</param>
        /// <returns>True if variables are enabled and configured</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public static bool HasVariablesEnabled(DbReactorConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            return config.EnableVariables && config.Variables != null && config.Variables.Any();
        }

        /// <summary>
        /// Resets the migration builder to force recreation on next access
        /// </summary>
        /// <param name="config">Configuration to reset</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public static void ResetMigrationBuilder(DbReactorConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            config.MigrationBuilder = null;
        }

        /// <summary>
        /// Creates a summary of the current configuration for diagnostics
        /// </summary>
        /// <param name="config">Configuration to summarize</param>
        /// <returns>Configuration summary string</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public static string CreateConfigurationSummary(DbReactorConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("DbReactor Configuration Summary:");
            summary.AppendLine($"- Script Providers: {GetScriptProviderCount(config)}");
            summary.AppendLine($"- Connection Manager: {(config.ConnectionManager != null ? "Configured" : "Not Configured")}");
            summary.AppendLine($"- Migration Journal: {(config.MigrationJournal != null ? "Configured" : "Not Configured")}");
            summary.AppendLine($"- Script Executor: {(config.ScriptExecutor != null ? "Configured" : "Not Configured")}");
            summary.AppendLine($"- Migration Builder: {(config.MigrationBuilder != null ? "Configured" : "Not Configured")}");
            summary.AppendLine($"- Downgrade Support: {(SupportsDowngrades(config) ? "Enabled" : "Disabled")}");
            summary.AppendLine($"- Variables: {(HasVariablesEnabled(config) ? $"Enabled ({config.Variables.Count} variables)" : "Disabled")}");
            summary.AppendLine($"- Execution Order: {config.ExecutionOrder}");
            
            return summary.ToString();
        }
    }
}