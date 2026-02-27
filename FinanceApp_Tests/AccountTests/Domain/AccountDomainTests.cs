using FluentAssertions;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Tests.AccountTests.Domain;
public class AccountDomainTests
{
    [Fact]
    public void Should_Update_Account_Name_When_Active()
    {
        var account = new Account(
            Guid.NewGuid(),
            "My Account",
            new Money(100, "USD"),
            AccountType.Checking
        );
        
        account.UpdateName("Updated Account Name");
        
        Assert.Equal("Updated Account Name", account.Name);
    }

    [Fact]
    public void Should_Throw_InvalidOperationException_When_Update_Inactive_Account_Name()
    {
        var account = new Account(
            Guid.NewGuid(),
            "My Account",
            new Money(0, "USD"),
            AccountType.Checking
        );
        account.Close();
        
        var action = () => account.UpdateName("Updated Account Name");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Account is closed.");
    }
    [Fact]
    public void Should_Throw_InvalidOperationException_When_Close_Account_That_Has_Balance()
    {
        var account = new Account(
            Guid.NewGuid(),
            "My Account",
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
            Guid.NewGuid(),
            "My Account",
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
            "My Account",
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
            "My Account",
            new Money(100, "USD"),
            AccountType.Checking
        );
        
        var action = () => account.ApplyDelta(new Money(50, "EUR"));

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Currency mismatch.");
    }

}
