using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceAppDatabase.DbConnection;
public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration? _config;
    private readonly string connectionString;

    public DbConnectionFactory(string ConnectionString)
    {
        connectionString = ConnectionString;
        _config = null;
    }
    public DbConnectionFactory(IConfiguration config)
    {
        _config = config;
        connectionString = _config.GetConnectionString("DbConnection")!;
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string is not configured.");

    }

    public async Task<SqlConnection> GetConnection(CancellationToken token = default)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(token);
        return connection;
    }

    public async Task<T> ExecuteInScopeAsync<T>(
      Func<SqlConnection, Task<T>> operation,
      CancellationToken token = default)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(token);
        return await operation(connection);
    }

    /// <summary>
    /// Reusable connection for non query operations
    /// </summary>
    /// <param name="operation">Delegate function to be executed with the reusable connection</param>
    /// <param name="token">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="OracleException">
    /// Thrown when a database error occurs during connection, transaction, or operation execution.
    /// Common error codes include:
    /// - ORA-00001: Unique constraint violation
    /// - ORA-01012: Not logged on
    /// - ORA-12170: Connection timeout
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection string is invalid or the connection cannot be opened.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="operation"/> is null.
    /// </exception>
    public async Task ExecuteInScopeAsync(
        Func<SqlConnection, Task> operation,
        CancellationToken token = default)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(token);
        await operation(connection);

    }

    /// <summary>
    /// Reusable connection for query / with return operations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="operation">Delegate function to be executed with the reusable connection and transaction</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>The connection as part of a delegate</returns>
    /// <exception cref="OracleException">
    /// Thrown when a database error occurs during connection, transaction, or operation execution.
    /// Common error codes include:
    /// - ORA-00001: Unique constraint violation
    /// - ORA-01012: Not logged on
    /// - ORA-12170: Connection timeout
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection string is invalid or the connection cannot be opened.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="operation"/> is null.
    /// </exception>
    public async Task<T> ExecuteInScopeWithTransactionAsync<T>(
        Func<SqlConnection, SqlTransaction, Task<T>> operation,
        CancellationToken token = default)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(token);
        using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        try
        {
            var result = await operation(connection, transaction);
            await transaction.CommitAsync(token);
            return result;

        }
        catch
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    /// <summary>
    /// Reusable connection for non query operations
    /// </summary>
    /// <param name="operation">Delegate function to be executed with the reusable connection and transaction</param>
    /// <param name="token">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="OracleException">
    /// Thrown when a database error occurs during connection, transaction, or operation execution.
    /// Common error codes include:
    /// - ORA-00001: Unique constraint violation
    /// - ORA-01012: Not logged on
    /// - ORA-12170: Connection timeout
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection string is invalid or the connection cannot be opened.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="operation"/> is null.
    /// </exception>
    public async Task ExecuteInScopeWithTransactionAsync(
        Func<SqlConnection, SqlTransaction, Task> operation,
        CancellationToken token = default)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(token);
        using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        try
        {
            await operation(connection, transaction);
            await transaction.CommitAsync(token);
        }
        catch
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }
}
