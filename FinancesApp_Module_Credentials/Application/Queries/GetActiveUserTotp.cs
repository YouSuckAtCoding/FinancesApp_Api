using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Module_Credentials.Application.Queries;
public class GetActiveUserTotp : IQuery<UserCredentialsTotp?>
{
    public Guid UserId { get; set; }
}
