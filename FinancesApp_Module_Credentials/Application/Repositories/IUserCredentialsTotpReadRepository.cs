using FinancesApp_Module_Credentials.Domain;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Module_Credentials.Application.Repositories;
public interface IUserCredentialsTotpReadRepository
{
    Task<UserCredentialsTotp?> GetActiveTotpByUserIdAsync(Guid userId,
                                                          SqlConnection? connection = null,
                                                          CancellationToken token = default);
}
