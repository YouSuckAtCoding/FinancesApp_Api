using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Account.Application.Queries;

public class GetTransactionHistory : IQuery<IReadOnlyList<AccountTransaction>>
{
    public Guid UserId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}
