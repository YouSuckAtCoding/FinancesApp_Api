using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain;

namespace FinancesApp_Module_Account.Application.Commands;
public record UpdateAccount : ICommand<bool>
{
    public required Account Account { get; init; }
}
