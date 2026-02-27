
using FinancesApp_Module_Account.Domain;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Module_Account.Application.Repositories;

public interface IAccountReadRepository
{
    Task<Account> GetAccountById(Guid id, 
                                 SqlConnection? connection = default, 
                                 CancellationToken token = default);
    Task<IReadOnlyList<Account>> GetAccounts(SqlConnection? connection = null,
                                             CancellationToken token = default);
    Task<IReadOnlyList<Account>> GetActiveAccounts(SqlConnection? connection = null,
                                                   CancellationToken token = default);
    Task<IReadOnlyList<Account>> GetAccountByType(AccountType type,
                                                  SqlConnection? connection = null,
                                                  CancellationToken token = default);
}