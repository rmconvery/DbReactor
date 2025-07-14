using DbReactor.Core.Abstractions;
using DbReactor.Core.Constants;
using DbReactor.Core.Execution;
using DbReactor.Core.Implementations.Logging;
using DbReactor.Core.Journaling;
using DbReactor.Core.Logging;
using DbReactor.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace DbReactor.MSSqlServer.Journaling
{
    /// <summary>
    /// SQL Server implementation of migration journaling
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
            string tableName = "MigrationJournal",
            ILogProvider logProvider = null)
        {
            _schemaName = SanitizeIdentifier(schemaName ?? DbReactorConstants.Defaults.DefaultSchemaName);
            _tableName = SanitizeIdentifier(tableName ?? DbReactorConstants.Defaults.JournalTableName);
            _fullTableName = $"[{_schemaName}].[{_tableName}]";
            _logProvider = logProvider ?? new NullLogProvider();
        }

        public void SetConnectionManager(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public IEnumerable<MigrationJournalEntry> GetExecutedMigrations()
        {
            EnsureConnectionManager();
            try
            {
                return _connectionManager.ExecuteCommandsWithManagedConnection(commandFactory =>
                {
                    IDbCommand command = commandFactory();
                    command.CommandText = $@"
                        SELECT [Id], [UpgradeScriptHash], [MigrationName], [DowngradeScript], [MigratedOn], [ExecutionTime]
                        FROM {_fullTableName}
                        ORDER BY [MigratedOn]";

                    List<MigrationJournalEntry> journalEntries = new List<MigrationJournalEntry>();
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
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
                    }
                    return journalEntries;
                });
            }
            catch (Exception ex)
            {
                _logProvider.WriteError("Failed to get executed migrations from journal.", ex);
                throw;
            }
        }

        public void StoreExecutedMigration(IMigration migration, MigrationResult result)
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
                _connectionManager.ExecuteCommandsWithManagedConnection(commandFactory =>
                {
                    IDbCommand command = commandFactory();
                    command.CommandText = $@"
                        INSERT INTO {_fullTableName} 
                        ([UpgradeScriptHash], [MigrationName], [DowngradeScript], [MigratedOn], [ExecutionTime])
                        VALUES (@UpgradeScriptHash, @MigrationName, @DowngradeScript, @MigratedOn, @ExecutionTime)";

                    AddParameter(command, "@UpgradeScriptHash", upgradeScript.Hash);
                    AddParameter(command, "@MigrationName", migration.Name);
                    AddParameter(command, "@DowngradeScript", migration.HasDowngrade ? migration.DowngradeScript.Script : null);
                    AddParameter(command, "@MigratedOn", DateTime.UtcNow);
                    AddParameter(command, "@ExecutionTime", (long)result.ExecutionTime.TotalMilliseconds);

                    command.ExecuteNonQuery();
                });
            }
            catch (Exception ex)
            {
                _logProvider.WriteError($"Failed to store executed migration '{migration.Name}'.", ex);
                throw;
            }
        }

        public void RemoveExecutedMigration(string upgradeScriptHash)
        {
            // Todo revist this. Using the hash for everything is going to fail eventually because it is not unique enough
            if (upgradeScriptHash == null) throw new ArgumentNullException(nameof(upgradeScriptHash));
            EnsureConnectionManager();

            try
            {
                _connectionManager.ExecuteCommandsWithManagedConnection(commandFactory =>
                {
                    IDbCommand command = commandFactory();
                    command.CommandText = $@"
                        DELETE FROM {_fullTableName} 
                        WHERE [UpgradeScriptHash] = @UpgradeScriptHash";

                    AddParameter(command, "@UpgradeScriptHash", upgradeScriptHash);

                    command.ExecuteNonQuery();
                });
            }
            catch (Exception ex)
            {
                _logProvider.WriteError($"Failed to remove executed migration with hash '{upgradeScriptHash}'.", ex);
                throw;
            }
        }

        public bool HasBeenExecuted(IMigration migration)
        {
            if (migration == null) throw new ArgumentNullException(nameof(migration));
            EnsureConnectionManager();

            IScript upgradeScript = migration.UpgradeScript;
            try
            {
                return _connectionManager.ExecuteCommandsWithManagedConnection(commandFactory =>
                {
                    IDbCommand command = commandFactory();
                    command.CommandText = $@"
                        SELECT COUNT(*) 
                        FROM {_fullTableName} 
                        WHERE [UpgradeScriptHash] = @UpgradeScriptHash";

                    AddParameter(command, "@UpgradeScriptHash", upgradeScript?.Hash ?? string.Empty);

                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                });
            }
            catch (Exception ex)
            {
                _logProvider.WriteError($"Failed to check if migration '{migration.Name}' has been executed.", ex);
                throw;
            }
        }

        public void EnsureTableExists(IConnectionManager connectionManager)
        {
            try
            {
                connectionManager.ExecuteCommandsWithManagedConnection(commandFactory =>
                {
                    // Check if table exists using parameterized query
                    IDbCommand checkCommand = commandFactory();
                    checkCommand.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_SCHEMA = @SchemaName 
                        AND TABLE_NAME = @TableName";
                    
                    IDbDataParameter schemaParam = checkCommand.CreateParameter();
                    schemaParam.ParameterName = "@SchemaName";
                    schemaParam.Value = _schemaName;
                    checkCommand.Parameters.Add(schemaParam);
                    
                    IDbDataParameter tableParam = checkCommand.CreateParameter();
                    tableParam.ParameterName = "@TableName";
                    tableParam.Value = _tableName;
                    checkCommand.Parameters.Add(tableParam);

                    bool tableExists = (int)checkCommand.ExecuteScalar() > 0;

                    if (!tableExists)
                    {
                        // Create the journal table
                        IDbCommand createCommand = commandFactory();
                        createCommand.CommandText = $@"
                            CREATE TABLE {_fullTableName} (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [UpgradeScriptHash] NVARCHAR(256) NOT NULL,
                                [MigrationName] NVARCHAR(512) NOT NULL,
                                [DowngradeScript] NVARCHAR(MAX) NULL,
                                [MigratedOn] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                                [ExecutionTime] BIGINT NOT NULL
                            )";
                        createCommand.ExecuteNonQuery();

                        // Create unique index on UpgradeScriptHash
                        IDbCommand indexCommand = commandFactory();
                        string indexName = GetSafeIndexName(_tableName, "UpgradeScriptHash");
                        indexCommand.CommandText = $@"
                            CREATE UNIQUE INDEX {indexName} 
                            ON {_fullTableName} ([UpgradeScriptHash])";
                        indexCommand.ExecuteNonQuery();

                        _logProvider.WriteInformation($"Created journal table {_fullTableName} and index {indexName}.");
                    }
                });
            }
            catch (Exception ex)
            {
                _logProvider.WriteError($"Failed to ensure journal table '{_fullTableName}' exists.", ex);
                throw;
            }
        }

        // Helper methods remain unchanged...

        private void EnsureConnectionManager()
        {
            if (_connectionManager == null)
                throw new InvalidOperationException("ConnectionManager must be set before using the journal. Call SetConnectionManager() first.");
        }

        private static void AddParameter(IDbCommand command, string name, object value)
        {
            IDbDataParameter param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            command.Parameters.Add(param);
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
