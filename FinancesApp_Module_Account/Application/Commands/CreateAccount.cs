using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain;

namespace FinancesApp_Module_Account.Application.Commands;

public record CreateAccount : ICommand<bool>
{
    public required Account Account { get; init; }
}