using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Api.Contracts.Requests.AccountRequests;

public record CreateAccountRequest(Guid? UserId, Money Balance, AccountType Type)
{
    public string Name { get; set; } = "";

    public Account MapToAccount()
    {
        return new Account(UserId,
                           Name,
                           Balance,
                           Type
        );
    }
}