using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Module_Credentials.Application.Queries;
public class GetUserCredentialsByUserId : IQuery<UserCredentials>
{
    public Guid UserId { get; set; }
}


