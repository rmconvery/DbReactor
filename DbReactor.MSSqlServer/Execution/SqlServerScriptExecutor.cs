using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Logging;
using DbReactor.Core.Models;
using DbReactor.Core.Utilities;
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
        private readonly Func<ILogProvider> _logProviderFactory;

        public SqlServerScriptExecutor() : this(SqlServerConstants.Defaults.CommandTimeout, (ILogProvider)null) { }

        public SqlServerScriptExecutor(TimeSpan commandTimeout) : this(commandTimeout, (ILogProvider)null) { }

        public SqlServerScriptExecutor(TimeSpan commandTimeout, ILogProvider logProvider)
            : this(commandTimeout, logProvider != null ? (Func<ILogProvider>)(() => logProvider) : null) { }

        public SqlServerScriptExecutor(TimeSpan commandTimeout, Func<ILogProvider> logProviderFactory)
        {
            _commandTimeout = commandTimeout;
            _logProviderFactory = logProviderFactory ?? (() => new NullLogProvider());
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
                await ExecuteNonQueryAsync(scriptWithoutGo, connectionManager, PathUtility.GetLeafName(script.Name), cancellationToken);
                return;
            }

            List<string> batches = SqlUtilities.ParseScriptIntoBatches(scriptContent);
            bool hasManualTransactions = SqlUtilities.ContainsTransactionStatements(scriptContent);

            await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                SqlConnection sqlConnection = (SqlConnection)connection;
                sqlConnection.FireInfoMessageEventOnUserErrors = true;
                string shortName = PathUtility.GetLeafName(script.Name);
                SqlInfoMessageEventHandler infoHandler = (sender, e) =>
                {
                    ILogProvider logProvider = _logProviderFactory();
                    foreach (SqlError error in e.Errors)
                    {
                        logProvider.WriteInformation($"[{shortName}] {error.Message}");
                    }
                };
                sqlConnection.InfoMessage += infoHandler;
                try
                {
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
                }
                finally
                {
                    sqlConnection.InfoMessage -= infoHandler;
                }
            }, cancellationToken);
        }

        private async Task ExecuteNonQueryAsync(string sql, IConnectionManager connectionManager, string scriptName, CancellationToken cancellationToken)
        {
            await connectionManager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                SqlConnection sqlConnection = (SqlConnection)connection;
                sqlConnection.FireInfoMessageEventOnUserErrors = true;
                SqlInfoMessageEventHandler infoHandler = (sender, e) =>
                {
                    ILogProvider logProvider = _logProviderFactory();
                    foreach (SqlError error in e.Errors)
                    {
                        logProvider.WriteInformation($"[{scriptName}] {error.Message}");
                    }
                };
                sqlConnection.InfoMessage += infoHandler;
                try
                {
                    using SqlCommand command = new SqlCommand(sql, sqlConnection)
                    {
                        CommandTimeout = (int)_commandTimeout.TotalSeconds
                    };
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                finally
                {
                    sqlConnection.InfoMessage -= infoHandler;
                }
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
