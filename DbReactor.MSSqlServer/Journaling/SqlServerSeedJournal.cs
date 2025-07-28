using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.MSSqlServer.Journaling
{
    /// <summary>
    /// SQL Server implementation of seed journal for tracking seed execution
    /// </summary>
    public class SqlServerSeedJournal : ISeedJournal
    {
        private const string TableName = "DbReactorSeedJournal";

        public async Task EnsureTableExistsAsync(IConnectionManager connectionManager, CancellationToken cancellationToken = default)
        {
            var createTableSql = $@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{TableName}]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[{TableName}] (
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [SeedName] [nvarchar](255) NOT NULL,
                        [Hash] [nvarchar](64) NOT NULL,
                        [Strategy] [nvarchar](50) NOT NULL,
                        [ExecutedOn] [datetime2](7) NOT NULL,
                        [Duration] [time](7) NOT NULL,
                        CONSTRAINT [PK_{TableName}] PRIMARY KEY CLUSTERED ([Id] ASC)
                    )
                END";

            await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                using var command = new SqlCommand(createTableSql, (SqlConnection)connection);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);
        }

        public async Task<bool> HasBeenExecutedAsync(ISeed seed, CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COUNT(1) FROM [{TableName}] WHERE [SeedName] = @SeedName";

            var result = await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                using var command = new SqlCommand(sql, (SqlConnection)connection);
                command.Parameters.AddWithValue("@SeedName", seed.Name);
                var count = (int)await command.ExecuteScalarAsync(cancellationToken);
                return count > 0;
            }, cancellationToken);

            return result;
        }

        public async Task<string> GetLastExecutedHashAsync(string seedName, CancellationToken cancellationToken = default)
        {
            var sql = $@"
                SELECT TOP 1 [Hash] 
                FROM [{TableName}] 
                WHERE [SeedName] = @SeedName 
                ORDER BY [ExecutedOn] DESC";

            var result = await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                using var command = new SqlCommand(sql, (SqlConnection)connection);
                command.Parameters.AddWithValue("@SeedName", seedName);
                var hash = await command.ExecuteScalarAsync(cancellationToken);
                return hash?.ToString();
            }, cancellationToken);

            return result;
        }

        public async Task RecordExecutionAsync(ISeed seed, DateTime executedOn, CancellationToken cancellationToken = default)
        {
            var sql = $@"
                INSERT INTO [{TableName}] ([SeedName], [Hash], [Strategy], [ExecutedOn], [Duration])
                VALUES (@SeedName, @Hash, @Strategy, @ExecutedOn, @Duration)";

            await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                using var command = new SqlCommand(sql, (SqlConnection)connection);
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
            var sql = $@"
                SELECT [SeedName], [Hash], [Strategy], [ExecutedOn], [Duration]
                FROM [{TableName}]
                ORDER BY [ExecutedOn] DESC";

            var result = await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                var entries = new List<SeedJournalEntry>();
                using var command = new SqlCommand(sql, (SqlConnection)connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

        private readonly IConnectionManager _connectionManager;

        public SqlServerSeedJournal(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }
    }
}