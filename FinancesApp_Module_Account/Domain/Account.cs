using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Module_Account.Domain;

public enum AccountType { Cash, Checking, CreditCard }
public enum AccountStatus { Active, Closed }
public enum OperationType { MoneyTransaction, Payment, CreditPurchase }
public sealed class Account
{
    public Guid Id { get; }
    public Guid? UserId{ get; }
    public string Name { get; private set; } = "";
    public Money Balance{ get; private set; }
    public Money CreditLimit { get; private set; }
    public Money CurrentDebt { get; private set; }   
    public DateTimeOffset? PaymentDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public AccountStatus Status { get; private set; }
    public AccountType Type{ get; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Account(Guid id,
                   Guid? userId,
                   string name,
                   Money balance,
                   AccountType type)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Type = type;
        ValidateInitalBalance(balance);
        CalculateCreditLimit(Balance);
        CurrentDebt = new Money(0m, balance.Currency);
    }
    public Account(Guid? userId, 
                   string name,
                   Money balance, 
                   AccountType type)
    {
        UserId = userId;
        Name = name;
        Type = type;
        ValidateInitalBalance(balance);
        CalculateCreditLimit(Balance);
        CurrentDebt = new Money(0m, balance.Currency);
    }

    public Account()
    {
        
    }
    private void ValidateInitalBalance(Money balance)
    {
        if(balance.Amount < 0 && Type != AccountType.CreditCard)
            throw new InvalidOperationException("Initial balance cannot be negative for non credit-card accounts.");
        Balance = balance;
    }

    public Account(Guid id, 
                   Guid? userId, 
                   string name, 
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
        Name = name;
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

    public void UpdateName(string name)
    {
        EnsureActive();
        Name = name;
    }

    public void ApplyDelta(Money delta, OperationType opType = OperationType.MoneyTransaction)
    {
        EnsureActive();
        EnsureCurrency(delta.Currency);

        if (Type == AccountType.CreditCard || opType == OperationType.CreditPurchase)
        {
            UpdateCredit(delta, opType);
            return;
        }

        var newBalance = Balance.Add(delta);

        if (Type == AccountType.Cash && newBalance.Amount < 0)
            throw new InvalidOperationException("Insufficient funds in cash account."); 

        Balance = newBalance;   
    }

    private void CalculateCreditLimit(Money balance)
    {

        if(Type == AccountType.CreditCard)
        {
            CreditLimit = new Money(4500m, balance.Currency);
            return;
        }

        if (balance.Amount <= 350)
            CreditLimit = new Money(500m, balance.Currency);
        else
            CreditLimit = new Money(decimal.Ceiling(balance.Amount * 2), balance.Currency);
    }

    private void UpdateCredit(Money delta, OperationType opType)
    {
        Money newDebt;

        if (opType == OperationType.Payment)
        {
            newDebt = CurrentDebt.Subtract(delta.Amount < 0 ? delta.Negate() : delta);
        }
        else
        {
            newDebt = CurrentDebt.Add(delta.Amount < 0 ? delta.Negate() : delta);
        }

        if (newDebt.Amount > CreditLimit.Amount)
            throw new InvalidOperationException("Credit limit exceeded.");

        if (newDebt.Amount < 0)
            CurrentDebt = new Money(0m, delta.Currency);
        else
            CurrentDebt = newDebt;
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

    public void Close()
    {
        EnsureActive();

        if (!Balance.IsZero)
            throw new InvalidOperationException("Account must have zero balance before closing.");

        Status = AccountStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
    }


}
