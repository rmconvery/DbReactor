using DbReactor.Core.Abstractions;
using DbReactor.MSSqlServer.Constants;
using DbReactor.Core.Execution;
using DbReactor.Core.Journaling;
using DbReactor.Core.Logging;
using DbReactor.Core.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.MSSqlServer.Journaling
{
    /// <summary>
    /// SQL Server async-first implementation of migration journaling
    /// </summary>
    public sealed class SqlServerScriptJournal : IMigrationJournal
    {
        private readonly string _tableName;
        private readonly string _schemaName;
        private readonly string _fullTableName;
        private readonly ILogProvider _logProvider;
        private IConnectionManager _connectionManager;

        public SqlServerScriptJournal(
            string schemaName = "dbo",
            string tableName = "__migration_journal",
            ILogProvider logProvider = null)
        {
            _schemaName = SanitizeIdentifier(schemaName ?? SqlServerConstants.Defaults.SchemaName);
            _tableName = SanitizeIdentifier(tableName ?? SqlServerConstants.Defaults.JournalTableName);
            _fullTableName = $"[{_schemaName}].[{_tableName}]";
            _logProvider = logProvider ?? new NullLogProvider();
        }

        public void SetConnectionManager(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public async Task EnsureTableExistsAsync(IConnectionManager connectionManager, CancellationToken cancellationToken = default)
        {
            try
            {
                await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
                {
                    await EnsureTableExistsInternalAsync((SqlConnection)connection, cancellationToken);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logProvider.WriteError($"Failed to ensure journal table '{_fullTableName}' exists.", ex);
                throw;
            }
        }

        public async Task StoreExecutedMigrationAsync(IMigration migration, MigrationResult result, CancellationToken cancellationToken = default)
        {
            if (migration == null) throw new ArgumentNullException(nameof(migration));
            if (result == null) throw new ArgumentNullException(nameof(result));
            EnsureConnectionManager();

            IScript upgradeScript = migration.UpgradeScript;
            if (upgradeScript == null || string.IsNullOrEmpty(upgradeScript.Hash))
            {
                _logProvider.WriteWarning("Attempted to store a migration with an empty hash. Operation skipped.");
                return;
            }

            try
            {
                await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
                {
                    using SqlCommand command = new SqlCommand($@"
                        INSERT INTO {_fullTableName} 
                        ([UpgradeScriptHash], [MigrationName], [DowngradeScript], [MigratedOn], [ExecutionTime])
                        VALUES (@UpgradeScriptHash, @MigrationName, @DowngradeScript, @MigratedOn, @ExecutionTime)", (SqlConnection)connection);

                    command.Parameters.AddWithValue("@UpgradeScriptHash", upgradeScript.Hash);
                    command.Parameters.AddWithValue("@MigrationName", migration.Name);
                    command.Parameters.AddWithValue("@DowngradeScript", migration.HasDowngrade ? migration.DowngradeScript.Script : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@MigratedOn", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@ExecutionTime", (long)result.ExecutionTime.TotalMilliseconds);

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logProvider.WriteError($"Failed to store executed migration '{migration.Name}'.", ex);
                throw;
            }
        }

        public async Task RemoveExecutedMigrationAsync(string upgradeScriptHash, CancellationToken cancellationToken = default)
        {
            if (upgradeScriptHash == null) throw new ArgumentNullException(nameof(upgradeScriptHash));
            EnsureConnectionManager();

            try
            {
                await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
                {
                    using SqlCommand command = new SqlCommand($@"
                        DELETE FROM {_fullTableName} 
                        WHERE [UpgradeScriptHash] = @UpgradeScriptHash", (SqlConnection)connection);

                    command.Parameters.AddWithValue("@UpgradeScriptHash", upgradeScriptHash);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logProvider.WriteError($"Failed to remove executed migration with hash '{upgradeScriptHash}'.", ex);
                throw;
            }
        }

        public async Task<IEnumerable<MigrationJournalEntry>> GetExecutedMigrationsAsync(CancellationToken cancellationToken = default)
        {
            EnsureConnectionManager();
            try
            {
                return await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
                {
                    using SqlCommand command = new SqlCommand($@"
                        SELECT [Id], [UpgradeScriptHash], [MigrationName], [DowngradeScript], [MigratedOn], [ExecutionTime]
                        FROM {_fullTableName}
                        ORDER BY [MigratedOn]", (SqlConnection)connection);

                    List<MigrationJournalEntry> journalEntries = new List<MigrationJournalEntry>();
                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        journalEntries.Add(new MigrationJournalEntry
                        {
                            Id = reader.GetInt32(0),
                            UpgradeScriptHash = reader.GetString(1),
                            MigrationName = reader.GetString(2),
                            DowngradeScript = reader.IsDBNull(3) ? null : reader.GetString(3),
                            MigratedOn = reader.GetDateTime(4),
                            ExecutionTime = TimeSpan.FromMilliseconds(reader.GetInt64(5))
                        });
                    }

                    return journalEntries;
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logProvider.WriteError("Failed to get executed migrations from journal.", ex);
                throw;
            }
        }

        public async Task<bool> HasBeenExecutedAsync(IMigration migration, CancellationToken cancellationToken = default)
        {
            if (migration == null) throw new ArgumentNullException(nameof(migration));
            EnsureConnectionManager();

            IScript upgradeScript = migration.UpgradeScript;
            try
            {
                return await _connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
                {
                    using SqlCommand command = new SqlCommand($@"
                        SELECT COUNT(*) 
                        FROM {_fullTableName} 
                        WHERE [UpgradeScriptHash] = @UpgradeScriptHash", (SqlConnection)connection);

                    command.Parameters.AddWithValue("@UpgradeScriptHash", upgradeScript?.Hash ?? string.Empty);
                    int count = (int)await command.ExecuteScalarAsync(cancellationToken);
                    return count > 0;
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logProvider.WriteError($"Failed to check if migration '{migration.Name}' has been executed.", ex);
                throw;
            }
        }

        private async Task EnsureTableExistsInternalAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            // Check if table exists
            using SqlCommand checkCommand = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = @SchemaName 
                AND TABLE_NAME = @TableName", connection);

            checkCommand.Parameters.AddWithValue("@SchemaName", _schemaName);
            checkCommand.Parameters.AddWithValue("@TableName", _tableName);

            bool tableExists = (int)await checkCommand.ExecuteScalarAsync(cancellationToken) > 0;

            if (!tableExists)
            {
                // Create the journal table
                using SqlCommand createCommand = new SqlCommand($@"
                    CREATE TABLE {_fullTableName} (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [UpgradeScriptHash] NVARCHAR(256) NOT NULL,
                        [MigrationName] NVARCHAR(512) NOT NULL,
                        [DowngradeScript] NVARCHAR(MAX) NULL,
                        [MigratedOn] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [ExecutionTime] BIGINT NOT NULL
                    )", connection);
                await createCommand.ExecuteNonQueryAsync(cancellationToken);

                // Create unique index on UpgradeScriptHash
                string indexName = GetSafeIndexName(_tableName, "UpgradeScriptHash");
                using SqlCommand indexCommand = new SqlCommand($@"
                    CREATE UNIQUE INDEX {indexName} 
                    ON {_fullTableName} ([UpgradeScriptHash])", connection);
                await indexCommand.ExecuteNonQueryAsync(cancellationToken);

                _logProvider.WriteInformation($"Created journal table {_fullTableName} and index {indexName}.");
            }
        }

        private void EnsureConnectionManager()
        {
            if (_connectionManager == null)
                throw new InvalidOperationException("ConnectionManager must be set before using the journal. Call SetConnectionManager() first.");
        }

        private static string SanitizeIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) || identifier.Contains(";") || identifier.Contains("--") || identifier.Contains("]"))
                throw new ArgumentException("Invalid SQL identifier.", nameof(identifier));
            return identifier;
        }

        private static string GetSafeIndexName(string tableName, string columnName)
        {
            string baseName = $"IX_{tableName}_{columnName}";
            return baseName.Length > 128 ? baseName.Substring(0, 128) : baseName;
        }
    }
}