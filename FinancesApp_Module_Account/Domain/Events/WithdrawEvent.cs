using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Account.Domain.Events;
public record WithdrawEvent(Guid EventId,
                            DateTimeOffset Timestamp,
                            Guid AccountId,
                            Guid UserId,
                            decimal Value, 
                            DateTimeOffset depositAt) : IDomainEvent
{
}
