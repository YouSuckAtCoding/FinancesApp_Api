using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Module_Account.Domain.Events;

public record ApplyDeltaErrorEvent(Guid EventId,
                                   DateTimeOffset Timestamp,
                                   Guid AccountId,
                                   Guid UserId,
                                   string ErrorMessage,
                                   Money AttemptedDelta,
                                   OperationType AttemptedOperationType,
                                   TransactionType AttemptedTransactionType) : IDomainEvent;
