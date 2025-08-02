using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.MSSqlServer.Journaling
{
    /// <summary>
    /// SQL Server implementation of seed journal for tracking seed execution
    /// </summary>
    public class SqlServerSeedJournal : ISeedJournal
    {
        private readonly string _schemaName;
        private readonly string _tableName;
        private readonly string _qualifiedTableName;
        private IConnectionManager _connectionManager;

        public SqlServerSeedJournal(string schemaName = "dbo", string tableName = "__seed_journal")
        {
            _schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _qualifiedTableName = $"[{_schemaName}].[{_tableName}]";
        }

        public SqlServerSeedJournal(IConnectionManager connectionManager, string schemaName = "dbo", string tableName = "__seed_journal")
            : this(schemaName, tableName)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public void SetConnectionManager(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public async Task EnsureTableExistsAsync(IConnectionManager connectionManager, CancellationToken cancellationToken = default)
        {
            var connManager = connectionManager ?? _connectionManager;
            if (connManager == null)
                throw new InvalidOperationException("No connection manager available. Either pass one to this method or set it via SetConnectionManager.");

            string createTableSql = $@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{_qualifiedTableName}') AND type in (N'U'))
                BEGIN
                    CREATE TABLE {_qualifiedTableName} (
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [SeedName] [nvarchar](255) NOT NULL,
                        [Hash] [nvarchar](64) NOT NULL,
                        [Strategy] [nvarchar](50) NOT NULL,
                        [ExecutedOn] [datetime2](7) NOT NULL,
                        [Duration] [time](7) NOT NULL,
                        CONSTRAINT [PK_{_tableName}] PRIMARY KEY CLUSTERED ([Id] ASC)
                    )
                END";

            await connManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                using SqlCommand command = new SqlCommand(createTableSql, (SqlConnection)connection);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);
        }

        public async Task<bool> HasBeenExecutedAsync(ISeed seed, CancellationToken cancellationToken = default)
        {
            if (_connectionManager == null)
                throw new InvalidOperationException("Connection manager not set. Call SetConnectionManager first.");

            string sql = $"SELECT COUNT(1) FROM {_qualifiedTableName} WHERE [SeedName] = @SeedName";

            bool result = await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                using SqlCommand command = new SqlCommand(sql, (SqlConnection)connection);
                command.Parameters.AddWithValue("@SeedName", seed.Name);
                int count = (int)await command.ExecuteScalarAsync(cancellationToken);
                return count > 0;
            }, cancellationToken);

            return result;
        }

        public async Task<string> GetLastExecutedHashAsync(string seedName, CancellationToken cancellationToken = default)
        {
            if (_connectionManager == null)
                throw new InvalidOperationException("Connection manager not set. Call SetConnectionManager first.");

            string sql = $@"
                SELECT TOP 1 [Hash] 
                FROM {_qualifiedTableName} 
                WHERE [SeedName] = @SeedName 
                ORDER BY [ExecutedOn] DESC";

            string result = await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                using SqlCommand command = new SqlCommand(sql, (SqlConnection)connection);
                command.Parameters.AddWithValue("@SeedName", seedName);
                object hash = await command.ExecuteScalarAsync(cancellationToken);
                return hash?.ToString();
            }, cancellationToken);

            return result;
        }

        public async Task RecordExecutionAsync(ISeed seed, DateTime executedOn, CancellationToken cancellationToken = default)
        {
            if (_connectionManager == null)
                throw new InvalidOperationException("Connection manager not set. Call SetConnectionManager first.");

            string sql = $@"
                INSERT INTO {_qualifiedTableName} ([SeedName], [Hash], [Strategy], [ExecutedOn], [Duration])
                VALUES (@SeedName, @Hash, @Strategy, @ExecutedOn, @Duration)";

            await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                using SqlCommand command = new SqlCommand(sql, (SqlConnection)connection);
                command.Parameters.AddWithValue("@SeedName", seed.Name);
                command.Parameters.AddWithValue("@Hash", seed.Hash);
                command.Parameters.AddWithValue("@Strategy", seed.Strategy.Name);
                command.Parameters.AddWithValue("@ExecutedOn", executedOn);
                command.Parameters.AddWithValue("@Duration", TimeSpan.Zero); // Duration tracking could be added later
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);
        }

        public async Task<IEnumerable<SeedJournalEntry>> GetExecutedSeedsAsync(CancellationToken cancellationToken = default)
        {
            if (_connectionManager == null)
                throw new InvalidOperationException("Connection manager not set. Call SetConnectionManager first.");

            string sql = $@"
                SELECT [SeedName], [Hash], [Strategy], [ExecutedOn], [Duration]
                FROM {_qualifiedTableName}
                ORDER BY [ExecutedOn] DESC";

            List<SeedJournalEntry> result = await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                List<SeedJournalEntry> entries = new List<SeedJournalEntry>();
                using SqlCommand command = new SqlCommand(sql, (SqlConnection)connection);
                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    entries.Add(new SeedJournalEntry
                    {
                        SeedName = reader.GetString(0),
                        Hash = reader.GetString(1),
                        Strategy = reader.GetString(2),
                        ExecutedOn = reader.GetDateTime(3),
                        Duration = reader.GetTimeSpan(4)
                    });
                }

                return entries;
            }, cancellationToken);

            return result;
        }
    }
}