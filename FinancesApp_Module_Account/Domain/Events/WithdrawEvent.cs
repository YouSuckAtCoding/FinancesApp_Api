using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Module_Account.Domain.Events;
public record WithdrawEvent(Guid EventId,
                            DateTimeOffset Timestamp,
                            Guid AccountId,
                            Guid UserId,
                            Money Amount, 
                            DateTimeOffset DepositAt) : IDomainEvent
{
}
