using FinancesApp_Api.Contracts.Requests.CredentialsRequests;
using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Api.Mapper;

public static class CredentialsMapper
{
    public static UserCredentials MapToUserCredentials(this CreateCredentialsRequest request, Guid userId)
    {
        return new UserCredentials(
            userId,
            request.Email,
            request.PlainPassword
        );
        
    }
}
