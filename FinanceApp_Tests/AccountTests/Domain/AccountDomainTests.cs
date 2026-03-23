using FluentAssertions;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Tests.AccountTests.Domain;
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
    public void Should_Throw_InvalidOperationException_When_ApplyDelta_With_No_CreditCard_Limit()
    {
        var account = new Account(
            Guid.NewGuid(),
            new Money(100, "USD"),
            AccountType.Checking
        );
        
        var action = () => account.ApplyDelta(new Money(600, "USD"), OperationType.CreditPurchase);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Credit limit exceeded.");
    }
    [Fact]
    public void Should_Throw_InvalidOperationException_When_Setting_Money_With_No_Currency()
    {
        var account = new Account(
            Guid.NewGuid(),
            new Money(100, "USD"),
            AccountType.Checking
        );

        var action = () => account.ApplyDelta(new Money(50, ""));

        action.Should().Throw<ArgumentException>()
            .WithMessage("Currency is required.");
    }
    [Fact]
    public void Should_Throw_InvalidOperationException_When_ApplyDelta_With_Currency_Mismatch()
    {
        var account = new Account(
            Guid.NewGuid(),
            new Money(100, "USD"),
            AccountType.Checking
        );
        
        var action = () => account.ApplyDelta(new Money(50, "EUR"));

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Currency mismatch.");
    }

    [Fact]
    void Should_Set_PaymentDate_And_DueDate_For_CreditCard_On_Creation()
    {
        var account = new Account(Guid.NewGuid(), new Money(0m, "USD"), AccountType.CreditCard);
        var now = DateTimeOffset.UtcNow;
        var expectedMonth = now.Month == 12 ? 1 : now.Month + 1;

        account.PaymentDate.Should().NotBeNull();
        account.DueDate.Should().NotBeNull();
        account.PaymentDate!.Value.Month.Should().Be(expectedMonth);
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
