using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Module_Credentials.Application.Repositories;
public interface IUserCredentialsRepository
{
    Task<Guid> CreateUserCredentialsAsync(UserCredentials credentials, CancellationToken token = default);
    Task<bool> DeleteUserCredentialsAsync(Guid userId, CancellationToken token = default);
    Task<bool> UpdatePasswordAsync(Guid userId, string newPasswordHash, CancellationToken token = default);
}