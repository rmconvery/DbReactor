using DbReactor.Core.Implementations.Logging;
using DbReactor.Core.Logging;
using DbReactor.Core.Provisioning;
using Microsoft.Data.SqlClient;
using System;

namespace DbReactor.MSSqlServer.Provisioning
{
    /// <summary>
    /// SQL Server implementation of database provisioner
    /// </summary>
    public class SqlServerDatabaseProvisioner : IDatabaseProvisioner
    {
        private readonly string _connectionString;
        private readonly ILogProvider _logProvider;

        public SqlServerDatabaseProvisioner(string connectionString, ILogProvider logProvider = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logProvider = logProvider ?? new NullLogProvider();
        }

        public bool DatabaseExists()
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_connectionString);
                string databaseName = builder.InitialCatalog;

                if (string.IsNullOrEmpty(databaseName))
                {
                    throw new InvalidOperationException("Connection string must specify a database name (Initial Catalog)");
                }

                string masterConnectionString = GetMasterConnectionString();

                using (SqlConnection connection = new SqlConnection(masterConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name = @dbName", connection))
                    {
                        cmd.Parameters.AddWithValue("@dbName", databaseName);
                        return (int)cmd.ExecuteScalar() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logProvider?.WriteError($"Error checking if database exists: {ex.Message}");
                throw;
            }
        }

        public void CreateDatabase(string template = null)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_connectionString);
                string databaseName = builder.InitialCatalog;

                if (string.IsNullOrEmpty(databaseName))
                {
                    throw new InvalidOperationException("Connection string must specify a database name (Initial Catalog)");
                }

                string masterConnectionString = GetMasterConnectionString();

                string createSql = template ?? $"CREATE DATABASE [{databaseName}]";
                if (template != null)
                {
                    createSql = string.Format(template, databaseName);
                }

                _logProvider?.WriteInformation($"Creating database: {databaseName}");

                using (SqlConnection connection = new SqlConnection(masterConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(createSql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                _logProvider?.WriteInformation($"Database created successfully: {databaseName}");
            }
            catch (Exception ex)
            {
                _logProvider?.WriteError($"Error creating database: {ex.Message}");
                throw;
            }
        }

        public void EnsureDatabaseExists(string template = null)
        {
            if (!DatabaseExists())
            {
                CreateDatabase(template);
            }
            else
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_connectionString);
                string databaseName = builder.InitialCatalog;
                _logProvider?.WriteInformation($"Database already exists: {databaseName}");
            }
        }

        private string GetMasterConnectionString()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_connectionString);
            builder.InitialCatalog = "master";
            return builder.ConnectionString;
        }
    }
}