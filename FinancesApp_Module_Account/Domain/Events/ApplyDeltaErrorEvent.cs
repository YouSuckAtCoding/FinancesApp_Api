using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Account.Domain.Events;

public record ApplyDeltaErrorEvent(Guid EventId,
                                   DateTimeOffset Timestamp,
                                   Guid AccountId,
                                   Guid UserId,
                                   string ErrorMessage,
                                   decimal AttemptedAmount,
                                   string AttemptedCurrency,
                                   OperationType AttemptedOperationType,
                                   TransactionType AttemptedTransactionType) : IDomainEvent;
