using Microsoft.Data.SqlClient;

namespace FinanceAppDatabase.DbConnection;
public interface IDbConnectionFactory
{
    Task ExecuteInScopeAsync(Func<SqlConnection, Task> operation, CancellationToken token = default);
    Task<T> ExecuteInScopeAsync<T>(Func<SqlConnection, Task<T>> operation, CancellationToken token = default);
    Task ExecuteInScopeWithTransactionAsync(Func<SqlConnection, SqlTransaction, Task> operation, CancellationToken token = default);
    Task<T> ExecuteInScopeWithTransactionAsync<T>(Func<SqlConnection, SqlTransaction, Task<T>> operation, CancellationToken token = default);
    Task<SqlConnection> GetConnection(CancellationToken token = default);
}