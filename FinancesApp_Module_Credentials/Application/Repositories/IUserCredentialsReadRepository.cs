using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Module_Credentials.Application.Repositories;
public interface IUserCredentialsReadRepository
{
    Task<UserCredentials> GetByLoginAsync(string login, CancellationToken token = default);
    Task<UserCredentials> GetByUserIdAsync(Guid userId, CancellationToken token = default);
}