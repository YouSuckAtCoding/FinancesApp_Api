using FinancesApp_Module_User.Domain;

namespace FinancesApp_Module_User.Application.Repositories;
public interface IUserRepository
{
    Task<Guid> CreateUserAsync(User user, CancellationToken token = default);
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken token = default);
    Task<bool> UpdateUserAsync(User user, CancellationToken token = default);
}