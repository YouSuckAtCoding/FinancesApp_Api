using FinancesApp_Api.Contracts.Requests.CredentialsRequests;
using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Api.Mapper;

public static class CredentialsMapper
{
    public static UserCredentials MapToUserCredentials(this CreateCredentialsRequest request)
    {
        return new UserCredentials(
            request.Login,
            request.PlainPassword
        );
        
    }
}
