using FinancesApp_Module_Credentials.Domain;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Module_Credentials.Application.Repositories;
public interface IUserCredentialsRepository
{
    Task<Guid> CreateUserCredentialsAsync(UserCredentials credentials, SqlConnection? connection = null, CancellationToken token = default);
    Task<bool> DeleteUserCredentialsAsync(Guid userId, SqlConnection? connection = null, CancellationToken token = default);
    Task<bool> UpdatePasswordAsync(Guid userId, string newPasswordHash, SqlConnection? connection = null, CancellationToken token = default);
}