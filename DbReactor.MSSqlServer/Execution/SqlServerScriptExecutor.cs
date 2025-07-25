using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using DbReactor.MSSqlServer.Constants;
using DbReactor.MSSqlServer.Utilities;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.MSSqlServer.Execution
{
    /// <summary>
    /// SQL Server async-first script executor
    /// </summary>
    public class SqlServerScriptExecutor : IScriptExecutor
    {
        private readonly TimeSpan _commandTimeout;

        public SqlServerScriptExecutor() : this(SqlServerConstants.Defaults.CommandTimeout) { }

        public SqlServerScriptExecutor(TimeSpan commandTimeout)
        {
            _commandTimeout = commandTimeout;
        }

        public async Task<MigrationResult> ExecuteAsync(IScript script, IConnectionManager connectionManager, CancellationToken cancellationToken = default)
        {
            MigrationResult result = new MigrationResult
            {
                Script = script,
                Successful = false
            };

            DateTime startTime = DateTime.UtcNow;

            try
            {
                await ExecuteScriptAsync(script, connectionManager, cancellationToken);
                result.Successful = true;
            }
            catch (Exception ex)
            {
                result.Error = ex;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                result.ExecutionTime = DateTime.UtcNow - startTime;
            }

            return result;
        }

        public async Task VerifySchemaAsync(IConnectionManager connectionManager, CancellationToken cancellationToken = default)
        {
            await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                using SqlCommand command = new SqlCommand("SELECT @@VERSION", (SqlConnection)connection);
                await command.ExecuteScalarAsync(cancellationToken);
            }, cancellationToken);
        }

        private async Task ExecuteScriptAsync(IScript script, IConnectionManager connectionManager, CancellationToken cancellationToken)
        {
            string scriptContent = script.Script;

            if (string.IsNullOrWhiteSpace(scriptContent))
                throw new InvalidOperationException("Script content is empty");

            if (SqlUtilities.IsEfTransactionScript(scriptContent))
            {
                string scriptWithoutGo = SqlUtilities.RemoveGoStatements(scriptContent);
                await ExecuteNonQueryAsync(scriptWithoutGo, connectionManager, cancellationToken);
                return;
            }

            List<string> batches = SqlUtilities.ParseScriptIntoBatches(scriptContent);
            bool hasManualTransactions = SqlUtilities.ContainsTransactionStatements(scriptContent);

            await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                SqlConnection sqlConnection = (SqlConnection)connection;
                if (hasManualTransactions)
                {
                    await ExecuteBatchesAsync(batches, sqlConnection, null, cancellationToken);
                }
                else
                {
                    using SqlTransaction transaction = sqlConnection.BeginTransaction();
                    try
                    {
                        await ExecuteBatchesAsync(batches, sqlConnection, transaction, cancellationToken);
                        transaction.Commit();
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { }
                        throw;
                    }
                }
            }, cancellationToken);
        }

        private async Task ExecuteNonQueryAsync(string sql, IConnectionManager connectionManager, CancellationToken cancellationToken)
        {
            await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                using SqlCommand command = new SqlCommand(sql, (SqlConnection)connection)
                {
                    CommandTimeout = (int)_commandTimeout.TotalSeconds
                };
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);
        }

        private async Task ExecuteBatchesAsync(IEnumerable<string> batches, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
        {
            int batchIndex = 0;
            foreach (string batch in batches)
            {
                batchIndex++;
                if (string.IsNullOrWhiteSpace(batch)) continue;

                try
                {
                    using SqlCommand command = new SqlCommand(batch, connection, transaction)
                    {
                        CommandTimeout = (int)_commandTimeout.TotalSeconds
                    };
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error executing batch {batchIndex}:\n{batch}\n\n{ex.Message}", ex);
                }
            }
        }
    }
}
