using FinancesApp_Module_Credentials.Domain;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Module_Credentials.Application.Repositories;
public interface IUserCredentialsReadRepository
{
    Task<UserCredentials> GetByLoginAsync(string login, SqlConnection? connection = null, CancellationToken token = default);
    Task<UserCredentials> GetByUserIdAsync(Guid userId, SqlConnection? connection = null, CancellationToken token = default);
}