using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain;

public record CreateAccount : ICommand<bool>
{
    public required Account Account { get; init; }
}