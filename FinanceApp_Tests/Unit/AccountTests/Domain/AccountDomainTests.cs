using FluentAssertions;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.Events;
using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Tests.Unit.AccountTests.Domain;
public class AccountDomainTests
{

    [Fact]
    public void Should_Throw_InvalidOperationException_When_Close_Account_That_Has_Balance()
    {
        var account = new Account(
            Guid.NewGuid(),
            new Money(100, "USD"),
            AccountType.Checking
        );
        var action = () => account.Close();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Account must have zero balance before closing.");
    }

    [Fact]
    public void Should_RaiseApplyDeltaError_WhenCreditLimitExceeded()
    {
        var account = new Account(
            Guid.NewGuid(),
            new Money(100, "USD"),
            AccountType.Checking
        );

        account.ApplyDelta(new Money(600m, "USD"), OperationType.CreditPurchase);

        var error = account.GetUncommittedEvents().OfType<ApplyDeltaErrorEvent>().Single();
        error.ErrorMessage.Should().Be("Credit limit exceeded.");
        error.AttemptedDelta.Should().Be(new Money(600m, "USD"));
        error.AttemptedOperationType.Should().Be(OperationType.CreditPurchase);
    }

    [Fact]
    public void Should_RaiseApplyDeltaError_WhenCurrencyMismatch()
    {
        var account = new Account(
            Guid.NewGuid(),
            new Money(100, "USD"),
            AccountType.Checking
        );

        account.ApplyDelta(new Money(50m, "EUR"));

        var error = account.GetUncommittedEvents().OfType<ApplyDeltaErrorEvent>().Single();
        error.ErrorMessage.Should().Be("Currency mismatch.");
    }

    [Fact]
    public void Should_RaiseApplyDeltaError_WhenAccountIsClosed()
    {
        var account = new Account(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(0m, "USD"),
            new Money(0m, "USD"),
            new Money(0m, "USD"),
            AccountStatus.Closed,
            AccountType.Checking,
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        account.ApplyDelta(new Money(50m, "USD"));

        var error = account.GetUncommittedEvents().OfType<ApplyDeltaErrorEvent>().Single();
        error.ErrorMessage.Should().Be("Account is closed.");
    }

    [Fact]
    public void Should_RaiseApplyDeltaError_WhenCreditCardUsesMoneyTransaction()
    {
        var account = new Account(
            Guid.NewGuid(),
            new Money(0m, "USD"),
            AccountType.CreditCard);

        account.ApplyDelta(new Money(50m, "USD"), OperationType.MoneyTransaction);

        var error = account.GetUncommittedEvents().OfType<ApplyDeltaErrorEvent>().Single();
        error.ErrorMessage.Should().Be("Credit card accounts only support credit card operations.");
    }

    [Fact]
    public void Should_RaiseApplyDeltaError_WhenCashWithdrawExceedsBalance()
    {
        var account = new Account(
            Guid.NewGuid(),
            new Money(50m, "USD"),
            AccountType.Cash);

        account.ApplyDelta(new Money(200m, "USD"));

        var error = account.GetUncommittedEvents().OfType<ApplyDeltaErrorEvent>().Single();
        error.ErrorMessage.Should().Be("Insufficient funds in cash account.");
    }

    [Fact]
    void Should_Set_PaymentDate_And_DueDate_For_CreditCard_On_Creation()
    {
        var account = new Account(Guid.NewGuid(), new Money(0m, "USD"), AccountType.CreditCard);
        var now = DateTimeOffset.UtcNow;

        account.DueDate.Should().NotBeNull();
        account.DueDate!.Value.Day.Should().Be(10);
    }

    [Fact]
    void Should_Not_Set_PaymentDates_For_Non_CreditCard()
    {
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Cash);

        account.PaymentDate.Should().BeNull();
        account.DueDate.Should().BeNull();
    }

    [Fact]
    void Should_Increase_Balance_On_Deposit()
    {
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);

        account.ApplyDelta(new Money(500m, "USD"), transactionType: TransactionType.Deposit);

        account.Balance.Amount.Should().Be(1500m);
    }
}
