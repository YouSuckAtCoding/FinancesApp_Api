using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;
using FinancesApp_Module_User.Domain;

namespace FinancesApp_Api.Contracts.Requests.AccountRequests;

public record UpdateAccountRequest(Guid AccountId,
                                   Guid UserId,                                   
                                   AccountStatus Status,
                                   AccountType Type,
                                   DateTimeOffset? PaymentDate)
{

    public Account MapToAccount()
    {
        return new Account(AccountId,
                           UserId,
                           Type,
                           PaymentDate!.Value);
    }
                           
}
