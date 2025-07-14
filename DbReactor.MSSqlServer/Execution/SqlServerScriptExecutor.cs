using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace DbReactor.MSSqlServer.Execution
{
    /// <summary>
    /// SQL Server specific script executor
    /// </summary>
    public class SqlServerScriptExecutor : IScriptExecutor
    {
        private readonly int _commandTimeout;

        public SqlServerScriptExecutor(int commandTimeout = 30)
        {
            _commandTimeout = commandTimeout;
        }

        public MigrationResult Execute(IScript script, IConnectionManager connectionManager)
        {
            MigrationResult result = new MigrationResult
            {
                Script = script,
                Successful = false
            };

            DateTime startTime = DateTime.UtcNow;

            try
            {
                connectionManager.ExecuteCommandsWithManagedConnection(commandFactory =>
                {
                    string scriptContent = script.Script;

                    if (string.IsNullOrWhiteSpace(scriptContent))
                    {
                        throw new InvalidOperationException("Script content is empty");
                    }

                    // Split script by GO statements (SQL Server batch separator)
                    string[] batches = SplitScriptIntoBatches(scriptContent);

                    foreach (string batch in batches)
                    {
                        if (string.IsNullOrWhiteSpace(batch)) continue;

                        IDbCommand command = commandFactory();
                        command.CommandText = batch.Trim();
                        command.CommandTimeout = _commandTimeout;

                        // Execute the batch - this handles both queries and non-queries
                        ExecuteBatch(command);
                    }
                });

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

        public void VerifySchema(IConnectionManager connectionManager)
        {
            connectionManager.ExecuteCommandsWithManagedConnection(commandFactory =>
            {
                IDbCommand command = commandFactory();
                command.CommandText = "SELECT @@VERSION";
                command.ExecuteScalar();
            });
        }

        private void ExecuteBatch(IDbCommand command)
        {
            string trimmedSql = command.CommandText.Trim().ToUpperInvariant();

            // Check if this looks like a query that returns results
            if (IsQueryStatement(trimmedSql))
            {
                // For queries, we execute but don't expect to process results in migration context
                // This allows SELECT statements in scripts (like existence checks) without failing
                using (IDataReader reader = command.ExecuteReader())
                {
                    // We could optionally log result counts or validate conditions here
                    // For now, just consume the results
                    while (reader.Read())
                    {
                        // Results are consumed but not processed
                    }
                }
            }
            else
            {
                // For DDL/DML statements, use ExecuteNonQuery
                command.ExecuteNonQuery();
            }
        }

        private bool IsQueryStatement(string sql)
        {
            // Simple heuristic to detect SELECT statements
            // This could be made more sophisticated if needed
            return sql.StartsWith("SELECT") ||
                   sql.StartsWith("WITH") ||  // CTEs
                   sql.StartsWith("EXEC") ||  // Could return results
                   sql.StartsWith("EXECUTE");
        }

        private string[] SplitScriptIntoBatches(string script)
        {
            // Split on GO statements (case insensitive, must be on its own line)
            // Also handle GO with optional line numbers (GO 5, etc.)
            return Regex.Split(script, @"^\s*GO\s*(\d+)?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }
    }
}

