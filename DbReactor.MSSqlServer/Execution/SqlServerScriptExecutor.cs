using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using DbReactor.MSSqlServer.Constants;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
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

        public SqlServerScriptExecutor() : this(SqlServerConstants.Defaults.CommandTimeout)
        {
        }

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
                using SqlCommand command = new SqlCommand("SELECT @@VERSION", (SqlConnection)connection);
                await command.ExecuteScalarAsync(cancellationToken);
            }, cancellationToken);
        }

        private async Task ExecuteScriptAsync(IScript script, IConnectionManager connectionManager, CancellationToken cancellationToken)
        {
            string scriptContent = script.Script;

            if (string.IsNullOrWhiteSpace(scriptContent))
                throw new InvalidOperationException("Script content is empty");

            // Special handling for EF/SSMS-style transaction scripts
            if (IsEfTransactionScript(scriptContent))
            {
                string scriptWithoutGo = RemoveGoStatements(scriptContent);
                await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
                {
                    SqlConnection sqlConnection = (SqlConnection)connection;
                    using SqlCommand command = new SqlCommand(scriptWithoutGo, sqlConnection);
                    command.CommandTimeout = (int)_commandTimeout.TotalSeconds;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }, cancellationToken);
                return;
            }

            // Normal batch execution
            List<string> batches = ParseScriptIntoBatches(scriptContent);

            await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                SqlConnection sqlConnection = (SqlConnection)connection;
                bool hasManualTransactions = ContainsTransactionStatements(scriptContent);

                if (hasManualTransactions)
                {
                    for (int i = 0; i < batches.Count; i++)
                    {
                        string batch = batches[i];
                        if (string.IsNullOrWhiteSpace(batch)) continue;

                        try
                        {
                            using SqlCommand command = new SqlCommand(batch, sqlConnection);
                            command.CommandTimeout = (int)_commandTimeout.TotalSeconds;
                            await command.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error executing batch {i + 1}:\n{batch}\n\n{ex.Message}", ex);
                        }
                    }
                }
                else
                {
                    using SqlTransaction transaction = sqlConnection.BeginTransaction();
                    try
                    {
                        for (int i = 0; i < batches.Count; i++)
                        {
                            string batch = batches[i];
                            if (string.IsNullOrWhiteSpace(batch)) continue;

                            try
                            {
                                using SqlCommand command = new SqlCommand(batch, sqlConnection, transaction);
                                command.CommandTimeout = (int)_commandTimeout.TotalSeconds;
                                await command.ExecuteNonQueryAsync(cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                try { transaction.Rollback(); } catch { }
                                throw new Exception($"Error executing batch {i + 1}:\n{batch}\n\n{ex.Message}", ex);
                            }
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        throw;
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Detects EF/SSMS-style transaction scripts that use GO between BEGIN TRANSACTION and COMMIT/ROLLBACK.
        /// </summary>
        private bool IsEfTransactionScript(string scriptContent)
        {
            // Looks for BEGIN TRANSACTION followed by GO, then COMMIT/ROLLBACK in a later batch
            // This is a heuristic, but matches EF and SSMS script output
            return Regex.IsMatch(
                scriptContent,
                @"BEGIN\s+TRAN(SACTION)?\b.*?^\s*GO\s*$.*?(COMMIT|ROLLBACK)\b.*?^\s*GO\s*$",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
        }

        /// <summary>
        /// Removes all GO batch separators from a script.
        /// </summary>
        private string RemoveGoStatements(string scriptContent)
        {
            // Remove lines that contain only GO or GO n (optionally with comments)
            return Regex.Replace(
                scriptContent,
                @"^\s*GO(?:\s+\d+)?\s*(?:--.*)?$",
                string.Empty,
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        private List<string> ParseScriptIntoBatches(string scriptContent)
        {
            // Try ScriptDom batch parsing first
            try
            {
                TSql150Parser parser = new TSql150Parser(true);
                using StringReader reader = new StringReader(scriptContent);
                TSqlFragment fragment = parser.Parse(reader, out IList<ParseError> errors);

                if (errors.Count == 0 && fragment is TSqlScript script)
                {
                    List<string> batches = new List<string>();
                    foreach (TSqlBatch batch in script.Batches)
                    {
                        string batchSql = GetSqlFromFragment(batch, scriptContent);
                        if (!string.IsNullOrWhiteSpace(batchSql))
                            batches.Add(batchSql);
                    }
                    if (batches.Count > 0)
                        return batches;
                }
            }
            catch
            {
                // Ignore and fallback
            }
            // Fallback: robust regex-based splitting
            return SplitOnGoStatements(scriptContent);
        }

        private List<string> SplitOnGoStatements(string scriptContent)
        {
            // Regex: matches lines with only GO or GO n (optionally with comments)
            Regex regex = new Regex(
                @"^\s*GO(?:\s+(\d+))?\s*(?:--.*)?$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            List<string> batches = new List<string>();
            int lastPos = 0;
            MatchCollection matches = regex.Matches(scriptContent);

            foreach (Match match in matches)
            {
                int len = match.Index - lastPos;
                if (len > 0)
                {
                    string batch = scriptContent.Substring(lastPos, len).Trim();
                    if (!string.IsNullOrWhiteSpace(batch))
                    {
                        int repeat = 1;
                        if (match.Groups[1].Success && int.TryParse(match.Groups[1].Value, out int n) && n > 1)
                            repeat = n;
                        for (int i = 0; i < repeat; i++)
                            batches.Add(batch);
                    }
                }
                lastPos = match.Index + match.Length;
            }
            // Add the last batch
            if (lastPos < scriptContent.Length)
            {
                string batch = scriptContent.Substring(lastPos).Trim();
                if (!string.IsNullOrWhiteSpace(batch))
                    batches.Add(batch);
            }
            return batches;
        }

        private string GetSqlFromFragment(TSqlFragment fragment, string originalScript)
        {
            if (fragment == null) return string.Empty;
            int startOffset = fragment.StartOffset;
            int length = fragment.FragmentLength;
            if (startOffset >= 0 && length > 0 && startOffset + length <= originalScript.Length)
                return originalScript.Substring(startOffset, length);
            return string.Empty;
        }

        private bool ContainsTransactionStatements(string scriptContent)
        {
            string upperScript = scriptContent.ToUpperInvariant();
            return upperScript.Contains("BEGIN TRANSACTION") ||
                   upperScript.Contains("BEGIN TRAN") ||
                   upperScript.Contains("COMMIT TRANSACTION") ||
                   upperScript.Contains("COMMIT TRAN") ||
                   upperScript.Contains("ROLLBACK TRANSACTION") ||
                   upperScript.Contains("ROLLBACK TRAN") ||
                   upperScript.Contains("COMMIT;") ||
                   upperScript.Contains("ROLLBACK;");
        }
    }
}
