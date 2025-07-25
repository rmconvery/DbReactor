using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using DbReactor.MSSqlServer.Constants;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Creates a new SQL Server script executor with default timeout
        /// </summary>
        public SqlServerScriptExecutor() : this(SqlServerConstants.Defaults.CommandTimeout)
        {
        }

        /// <summary>
        /// Creates a new SQL Server script executor with specified timeout
        /// </summary>
        /// <param name="commandTimeout">Command timeout for SQL operations</param>
        public SqlServerScriptExecutor(TimeSpan commandTimeout)
        {
            _commandTimeout = commandTimeout;
        }

        public async Task<MigrationResult> ExecuteAsync(IScript script, IConnectionManager connectionManager, CancellationToken cancellationToken = default)
        {
            var result = new MigrationResult
            {
                Script = script,
                Successful = false
            };

            var startTime = DateTime.UtcNow;

            try
            {
                await ExecuteScriptAsync(script, connectionManager, cancellationToken);
                result.Successful = true;
            }
            catch (Exception ex)
            {
                result.Error = ex;
                result.ErrorMessage = ex.Message;
                result.Successful = false;
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
                using var command = new SqlCommand("SELECT @@VERSION", (SqlConnection)connection);
                await command.ExecuteScalarAsync(cancellationToken);
            }, cancellationToken);
        }

        private async Task ExecuteScriptAsync(IScript script, IConnectionManager connectionManager, CancellationToken cancellationToken)
        {
            string scriptContent = script.Script;

            if (string.IsNullOrWhiteSpace(scriptContent))
            {
                throw new InvalidOperationException("Script content is empty");
            }

            // Split script by GO statements (SQL Server batch separator)
            string[] batches = SplitScriptIntoBatches(scriptContent);

            await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                var sqlConnection = (SqlConnection)connection;
                SqlTransaction transaction = null;
                bool hasManualTransaction = ContainsManualTransactionControl(scriptContent);

                try
                {
                    // Only start an automatic transaction if the script doesn't manage its own
                    if (!hasManualTransaction)
                    {
                        transaction = sqlConnection.BeginTransaction();
                    }

                    foreach (string batch in batches)
                    {
                        if (string.IsNullOrWhiteSpace(batch)) continue;

                        using var command = new SqlCommand(batch.Trim(), sqlConnection);
                        command.CommandTimeout = (int)_commandTimeout.TotalSeconds;
                        
                        // Only set transaction if we're managing it automatically
                        if (transaction != null)
                        {
                            command.Transaction = transaction;
                        }

                        await ExecuteBatchAsync(command, cancellationToken);
                    }

                    // Only commit if we started the transaction
                    if (transaction != null)
                    {
                        transaction.Commit();
                    }
                }
                catch
                {
                    // Only rollback if we started the transaction
                    if (transaction != null)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                            // Ignore rollback errors - connection will be disposed anyway
                        }
                    }
                    throw;
                }
                finally
                {
                    transaction?.Dispose();
                }
            }, cancellationToken);
        }

        private async Task ExecuteBatchAsync(SqlCommand command, CancellationToken cancellationToken)
        {
            string trimmedSql = command.CommandText.Trim().ToUpperInvariant();

            // Check if this looks like a query that returns results
            if (IsQueryStatement(trimmedSql))
            {
                // For queries, we execute but don't expect to process results in migration context
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    // Results are consumed but not processed
                }
            }
            else
            {
                // For DDL/DML statements, use ExecuteNonQuery
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        private bool IsQueryStatement(string sql)
        {
            // Simple heuristic to detect SELECT statements
            return sql.StartsWith("SELECT") ||
                   sql.StartsWith("WITH") ||  // CTEs
                   sql.StartsWith("EXEC") ||  // Could return results
                   sql.StartsWith("EXECUTE");
        }

        private string[] SplitScriptIntoBatches(string script)
        {
            // Split on GO statements (case insensitive, must be on its own line)
            return Regex.Split(script, @"^\s*GO\s*(\d+)?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        private bool ContainsManualTransactionControl(string script)
        {
            // Check if the script contains explicit transaction control statements
            var upperScript = script.ToUpperInvariant();
            
            bool hasBeginTransaction = Regex.IsMatch(upperScript, @"\bBEGIN\s+TRAN(SACTION)?\b", RegexOptions.IgnoreCase);
            bool hasCommitTransaction = Regex.IsMatch(upperScript, @"\bCOMMIT(\s+TRAN(SACTION)?)?\b", RegexOptions.IgnoreCase);
            bool hasRollbackTransaction = Regex.IsMatch(upperScript, @"\bROLLBACK(\s+TRAN(SACTION)?)?\b", RegexOptions.IgnoreCase);
            
            return hasBeginTransaction || hasCommitTransaction || hasRollbackTransaction;
        }
    }
}