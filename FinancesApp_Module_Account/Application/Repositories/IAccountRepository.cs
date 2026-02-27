using FinancesApp_Module_Account.Domain;
using Microsoft.Data.SqlClient;

public interface IAccountRepository
{
    Task<bool> CreateAccountAsync(Account account, SqlConnection? connection = null, CancellationToken token = default);
    Task<bool> DeleteAccountAsync(Guid accountId, SqlConnection? connection = null, CancellationToken token = default);
    Task<bool> UpdateAccountAsync(Account account, SqlConnection? connection = null, CancellationToken token = default);
}