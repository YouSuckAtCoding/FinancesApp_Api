using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_CQRS.Commands;
public record DeleteAccount : ICommand<bool>
{
    public required Guid AccountId { get; init; }
}
