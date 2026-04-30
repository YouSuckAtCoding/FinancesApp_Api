using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Module_Account.Application.Queries;

public enum TransactionKind
{
    Deposit,
    Withdraw,
    CreditCardPayment,
    CreditCardChange
}

public record AccountTransaction(Guid EventId,
                                 Guid AccountId,
                                 DateTimeOffset Timestamp,
                                 TransactionKind Kind,
                                 Money Amount);
