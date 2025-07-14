using System;

namespace DbReactor.Core.Exceptions
{
    /// <summary>
    /// Base exception for all DbReactor-related errors
    /// </summary>
    public class DbReactorException : Exception
    {
        public string Operation { get; }
        public string ScriptName { get; }

        public DbReactorException(string message) : base(message)
        {
        }

        public DbReactorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DbReactorException(string message, string operation, string scriptName = null) : base(message)
        {
            Operation = operation;
            ScriptName = scriptName;
        }

        public DbReactorException(string message, string operation, string scriptName, Exception innerException) 
            : base(message, innerException)
        {
            Operation = operation;
            ScriptName = scriptName;
        }

        public override string ToString()
        {
            string baseMessage = base.ToString();
            
            if (!string.IsNullOrEmpty(Operation))
            {
                baseMessage += $"\nOperation: {Operation}";
            }
            
            if (!string.IsNullOrEmpty(ScriptName))
            {
                baseMessage += $"\nScript: {ScriptName}";
            }
            
            return baseMessage;
        }
    }

    /// <summary>
    /// Exception thrown when migration script execution fails
    /// </summary>
    public class MigrationExecutionException : DbReactorException
    {
        public MigrationExecutionException(string message, string scriptName) 
            : base(message, "Migration Execution", scriptName)
        {
        }

        public MigrationExecutionException(string message, string scriptName, Exception innerException) 
            : base(message, "Migration Execution", scriptName, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when configuration is invalid
    /// </summary>
    public class ConfigurationException : DbReactorException
    {
        public ConfigurationException(string message) : base(message, "Configuration")
        {
        }

        public ConfigurationException(string message, Exception innerException) 
            : base(message, "Configuration", null, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when script discovery fails
    /// </summary>
    public class ScriptDiscoveryException : DbReactorException
    {
        public ScriptDiscoveryException(string message) : base(message, "Script Discovery")
        {
        }

        public ScriptDiscoveryException(string message, Exception innerException) 
            : base(message, "Script Discovery", null, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when journal operations fail
    /// </summary>
    public class JournalException : DbReactorException
    {
        public JournalException(string message) : base(message, "Journal Operation")
        {
        }

        public JournalException(string message, Exception innerException) 
            : base(message, "Journal Operation", null, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when database connection fails
    /// </summary>
    public class DatabaseConnectionException : DbReactorException
    {
        public string ConnectionString { get; }

        public DatabaseConnectionException(string message, string connectionString = null) 
            : base(message, "Database Connection")
        {
            ConnectionString = connectionString;
        }

        public DatabaseConnectionException(string message, string connectionString, Exception innerException) 
            : base(message, "Database Connection", null, innerException)
        {
            ConnectionString = connectionString;
        }

        public override string ToString()
        {
            string baseMessage = base.ToString();
            
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                // Don't log the full connection string for security
                baseMessage += $"\nConnection: [REDACTED]";
            }
            
            return baseMessage;
        }
    }
}