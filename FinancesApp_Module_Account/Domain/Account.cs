using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain.Events;
using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Module_Account.Domain;

public enum AccountType { Cash, Checking, CreditCard }
public enum AccountStatus { Active, Closed }
public enum OperationType { MoneyTransaction, Payment, CreditPurchase }
public enum TransactionType { Withdraw, Deposit }
public sealed class Account : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid UserId{ get; private set; }    
    public Money Balance{ get; private set; }
    public Money CreditLimit { get; private set; }
    public Money CurrentDebt { get; private set; }   
    public DateTimeOffset? PaymentDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public AccountStatus Status { get; private set; }
    public AccountType Type{ get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Account(Guid id,
                   Guid userId,
                   Money balance,
                   AccountType type) 
        => Raise(new UpdatedAccountEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, id, userId, balance, new Money(0m, balance.Currency), type));
    

    public Account(Guid userId,
                   Money balance,
                   AccountType type) 
        => Raise(new AccountCreatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), userId, balance, new Money(0m, balance.Currency) ,type));
    
    public Account(Guid accountId)
    {
        Id = accountId;  
    }
   
    public Account()
    {
        
    }

    public Account(Guid id, 
                   Guid userId, 
                   Money balance, 
                   Money creditLimit, 
                   Money currentDebt,
                   AccountStatus status, 
                   AccountType type,
                   DateTimeOffset? paymentDate,
                   DateTimeOffset? dueDate,
                   DateTimeOffset createdAt, 
                   DateTimeOffset? closedAt)
    {
        Id = id;
        UserId = userId;
        Balance = balance;
        CreditLimit = creditLimit;
        CurrentDebt = currentDebt;
        Status = status;
        Type = type;
        PaymentDate = paymentDate;
        DueDate = dueDate;
        CreatedAt = createdAt;
        ClosedAt = closedAt;
    }

    public void ApplyDelta(Money delta, 
                           OperationType opType = OperationType.MoneyTransaction,
                           TransactionType transactionType = TransactionType.Withdraw)
    {
        EnsureActive();
        EnsureCurrency(delta.Currency);

        if (Type == AccountType.CreditCard || opType == OperationType.CreditPurchase)
        {
            UpdateCredit(delta, opType);
            return;
        }
        
        if (transactionType == TransactionType.Withdraw)
            Raise(new WithdrawEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, UserId, delta.Amount, DateTimeOffset.UtcNow));
        else
            Raise(new DepositEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, UserId, delta.Amount, DateTimeOffset.UtcNow));

    }

    public void Close()
    {
        EnsureActive();

        if (!Balance.IsZero)
            throw new InvalidOperationException("Account must have zero balance before closing.");

        Raise(new AccountClosedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, UserId));
    }

    private void CalculateCreditLimit(Money balance)
    {

        if(Type == AccountType.CreditCard)
        {
            CreditLimit = new Money(4500m, balance.Currency);
            return;
        }

        if (balance.Amount <= 350)
            Raise(new CalculatedCreditLimitEvent(Guid.NewGuid(),
                                                 DateTimeOffset.UtcNow,
                                                 Id,
                                                 UserId,
                                                 new Money(500m, balance.Currency)));
        else
            Raise(new CalculatedCreditLimitEvent(Guid.NewGuid(),
                                                 DateTimeOffset.UtcNow,
                                                 Id,
                                                 UserId,
                                                 new Money(decimal.Ceiling(balance.Amount * 2), balance.Currency)));
    }

    protected override void Apply(IDomainEvent evt)
    {
        switch (evt)
        {
            case DepositEvent e:
                Balance = Balance.Add(new Money(e.Value, Balance.Currency));
                break;
            case WithdrawEvent e:
                var newBalance = Balance.Subtract(new Money(e.Value, Balance.Currency));
                
                if (Type == AccountType.Cash && newBalance.Amount < 0)
                    throw new InvalidOperationException("Insufficient funds in cash account.");

                Balance = newBalance;
                break;
            case CalculatedCreditLimitEvent e:
                CreditLimit = new Money(e.Value.Amount, e.Value.Currency);
                break;
            case CreditUpdatedEvent e:
                CurrentDebt = new Money(e.Value.Amount, e.Value.Currency);
                break;
            case AccountClosedEvent:
                Status = AccountStatus.Closed;
                ClosedAt = DateTimeOffset.UtcNow;
                break;
            case AccountCreatedEvent e:
                Id = e.Id;
                UserId = e.userId;
                Type = e.type;
                if (ValidateInitialBalance(e.balance))
                    Balance = e.balance;
                CalculateCreditLimit(Balance);
                CurrentDebt = e.debt;
                CreatedAt = e.Timestamp;
                break;
            case UpdatedAccountEvent e:
                Id = e.Id;
                UserId = e.userId;
                Type = e.type;
                if (ValidateInitialBalance(e.balance))
                    Balance = e.balance;
                CalculateCreditLimit(Balance);
                CurrentDebt = e.debt;
                UpdatedAt = e.Timestamp;
                break;
            default:
                throw new NotImplementedException(string.Format("No event handler for selected operation {0}", evt.GetType().Name));
        }
    }

    public override void RebuildFromEvents(List<IDomainEvent> events)
    {
        foreach (var evt in events)
            Apply(evt);
        SetAggregateVersions(events.Count);
    }

    private void UpdateCredit(Money delta, OperationType opType)
    {
        Money newDebt;

        if (opType == OperationType.Payment)
            newDebt = CurrentDebt.Subtract(delta.Amount < 0 ? delta.Negate() : delta);
        else
            newDebt = CurrentDebt.Add(delta.Amount < 0 ? delta.Negate() : delta);
        
        if (newDebt.Amount > CreditLimit.Amount)
            throw new InvalidOperationException("Credit limit exceeded.");

        var debt = newDebt.Amount < 0 ? new Money(0m, delta.Currency) : newDebt;

        Raise(new CreditUpdatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, UserId, debt, CurrentDebt, newDebt));
    }
  
    private void EnsureCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.");

        bool ok = Type switch
        {
            AccountType.Cash or AccountType.Checking
                => string.Equals(Balance.Currency, currency, StringComparison.OrdinalIgnoreCase),

            AccountType.CreditCard
                => string.Equals(CreditLimit.Currency, currency, StringComparison.OrdinalIgnoreCase),

            _ => throw new ArgumentOutOfRangeException("Unknown account type.")
        };

        if (!ok)
            throw new InvalidOperationException("Currency mismatch.");
    }

    private void EnsureActive()
    {
        if(Status == AccountStatus.Closed)
            throw new InvalidOperationException("Account is closed.");
    }

    private bool ValidateInitialBalance(Money balance)
    {
        if (balance.Amount < 0 && Type != AccountType.CreditCard)
            throw new InvalidOperationException("Initial balance cannot be negative for non credit-card accounts.");
        return true;
    }
}
