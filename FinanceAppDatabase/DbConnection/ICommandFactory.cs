using Microsoft.Data.SqlClient;

namespace FinanceAppDatabase.DbConnection;
public interface ICommandFactory
{
    Task ExecuteAsync(string commandText, CreateSqlCommandOptions options, Func<SqlCommand, Task> operation, CancellationToken token = default);
    Task ExecuteAsync(string commandText, SqlConnection? connection, CreateSqlCommandOptions options, Func<SqlCommand, Task> operation, CancellationToken token = default);
    Task ExecuteAsync(string commandText, SqlConnection? connection, SqlTransaction transaction, CreateSqlCommandOptions options, Func<SqlCommand, Task> operation, CancellationToken token = default);
    Task<T> ExecuteAsync<T>(string commandText, CreateSqlCommandOptions options, Func<SqlCommand, Task<T>> operation, CancellationToken token = default);
    Task<T> ExecuteAsync<T>(string commandText, SqlConnection? connection, CreateSqlCommandOptions options, Func<SqlCommand, Task<T>> operation, CancellationToken token = default);
    Task<T> ExecuteAsync<T>(string commandText, SqlConnection? connection, SqlTransaction transaction, CreateSqlCommandOptions options, Func<SqlCommand, Task<T>> operation, CancellationToken token = default);
}