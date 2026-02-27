using FinancesApp_Module_User.Domain;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Module_User.Application.Repositories;
public interface IUserReadRepository
{
    Task<User> GetUserByEmail(string v, SqlConnection? connection = default, CancellationToken token = default);
    Task<User> GetUserById(Guid insertedId, SqlConnection? connection = default, CancellationToken token = default);
    Task<IReadOnlyList<User>> GetUsers(SqlConnection? connection = default, CancellationToken token = default);
    Task<IReadOnlyList<User>> GetUsersRegisteredAfter(DateTimeOffset filterDate, SqlConnection? connection = default, CancellationToken token = default);
}