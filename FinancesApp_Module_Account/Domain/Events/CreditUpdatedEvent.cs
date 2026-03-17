using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Module_Account.Domain.Events;
internal record CreditUpdatedEvent(Guid EventId,
                                   DateTimeOffset Timestamp,
                                   Guid AccountId,
                                   Guid UserId,
                                   Money Value,
                                   Money PreviousDebt,
                                   Money CurrentDebt) : IDomainEvent
{
}
