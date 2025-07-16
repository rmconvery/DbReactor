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
                foreach (string batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;

                    using var command = new SqlCommand(batch.Trim(), (SqlConnection)connection);
                    command.CommandTimeout = (int)_commandTimeout.TotalSeconds;

                    await ExecuteBatchAsync(command, cancellationToken);
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
    }
}