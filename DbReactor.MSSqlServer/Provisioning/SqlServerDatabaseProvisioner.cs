using DbReactor.Core.Logging;
using DbReactor.Core.Provisioning;
using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.MSSqlServer.Provisioning
{
    /// <summary>
    /// SQL Server async-first implementation of database provisioner
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

        public async Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default)
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
                    await connection.OpenAsync(cancellationToken);
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name = @dbName", connection))
                    {
                        cmd.Parameters.AddWithValue("@dbName", databaseName);
                        var result = await cmd.ExecuteScalarAsync(cancellationToken);
                        return (int)result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logProvider?.WriteError($"Error checking if database exists: {ex.Message}");
                throw;
            }
        }

        public async Task CreateDatabaseAsync(string template = null, CancellationToken cancellationToken = default)
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
                    await connection.OpenAsync(cancellationToken);
                    using (SqlCommand cmd = new SqlCommand(createSql, connection))
                    {
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
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

        public async Task EnsureDatabaseExistsAsync(string template = null, CancellationToken cancellationToken = default)
        {
            if (!await DatabaseExistsAsync(cancellationToken))
            {
                await CreateDatabaseAsync(template, cancellationToken);
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