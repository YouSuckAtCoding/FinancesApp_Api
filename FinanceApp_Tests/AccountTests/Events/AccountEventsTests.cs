using FinancesApp_CQRS.EventStore;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.Events;
using FinancesApp_Module_Account.Domain.ValueObjects;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;

namespace FinancesApp_Tests.AccountTests.Events;
public class AccountEventsTests : IClassFixture<SqlFixture>
{
    private readonly EventStore _eventStore;
    private readonly SqlFixture _sqlFixture;

    public AccountEventsTests(SqlFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
        _eventStore = new EventStore(_sqlFixture.CommandFactory, _sqlFixture.ConnectionFactory);
    }

    [Fact]
    async Task Should_Persist_Account_And_Rebuild_Correctly_From_EventStore()
    {
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);
        var rebuiltAccount = new Account();
        
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion, default);
        var events = await _eventStore.Load(account.Id, default);
        
        rebuiltAccount.RebuildFromEvents(events);

        rebuiltAccount.Id.Should().Be(account.Id);
        rebuiltAccount.Balance.Should().Be(account.Balance);
        rebuiltAccount.Type.Should().Be(account.Type);
        rebuiltAccount.CurrentVersion.Should().Be(account.NextVersion);
    }

    [Fact]
    async Task Should_Fail_To_Register_Due_To_Version_Mismatch()
    {
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);

        var result = async () => await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion + 1, default);
        
        await result.Should().ThrowAsync<ConcurrencyException>();
    }

    [Fact]
    async Task Should_Allow_Concurrent_Appends_With_Correct_Versioning()
    {
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);

        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion, default);

        var concurrentAccount = new Account();

        var events = await _eventStore.Load(account.Id, default);

        concurrentAccount.RebuildFromEvents(events);        

        concurrentAccount.ApplyDelta(new Money(500m, "USD"));
        
        var result = async () => await _eventStore.Append(concurrentAccount.Id, concurrentAccount.GetUncommittedEvents(), concurrentAccount.CurrentVersion, default);
        
        await result.Should().NotThrowAsync<ConcurrencyException>();
    }

    [Fact]
    async Task Should_Persist_Account_Upudate_Information_And_Rebuild_Correctly_From_EventStore()
    {
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);
        var rebuiltAccount = new Account();

        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion, default);
        
        var events = await _eventStore.Load(account.Id, default);
        
        rebuiltAccount.RebuildFromEvents(events);

        rebuiltAccount.ApplyDelta(new Money(500m, "USD"), OperationType.MoneyTransaction, TransactionType.Deposit);
        
        await _eventStore.Append(rebuiltAccount.Id, rebuiltAccount.GetUncommittedEvents(), rebuiltAccount.CurrentVersion, default);

        var finalAccount = new Account();   
        finalAccount.RebuildFromEvents(await _eventStore.Load(account.Id, default));

        finalAccount.CurrentVersion.Should().BeGreaterThan(rebuiltAccount.CurrentVersion);
        finalAccount.Id.Should().Be(rebuiltAccount.Id);
        finalAccount.UpdatedAt.Should().NotBe(null);
       
    }

    [Fact]
    void Should_Update_CurrentDebt_After_Payment_Without_Raising_Extra_Events()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), new Money(0m, "USD"), AccountType.CreditCard);
        account.ClearUncommittedEvents();

        account.ApplyDelta(new Money(1000m, "USD"), OperationType.CreditPurchase);
        account.ClearUncommittedEvents();

        // Act
        account.PayCreditCardDebt(new Money(600m, "USD"));

        // Assert — debt must reflect the payment
        // Fails if UpdateCredit(raiseEvent: false) doesn't assign CurrentDebt
        account.CurrentDebt.Amount.Should().Be(400m);

        // Also assert no extra CreditUpdatedEvent was raised (only CredidCardStatementPaymentEvent)
        account.GetUncommittedEvents()
               .Should().NotContain(e => e is CreditUpdatedEvent,
                   because: "payment path should not raise CreditUpdatedEvent");
    }

    [Fact]
    async Task Should_Detect_Concurrency_Conflict_On_Simultaneous_Writes()
    {
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion, default);

        var userA = new Account();
        userA.RebuildFromEvents(await _eventStore.Load(account.Id, default));

        var userB = new Account();
        userB.RebuildFromEvents(await _eventStore.Load(account.Id, default));

        userA.ApplyDelta(new Money(100m, "USD"), transactionType: TransactionType.Deposit);
        await _eventStore.Append(userA.Id, userA.GetUncommittedEvents(), userA.CurrentVersion, default);

        userB.ApplyDelta(new Money(200m, "USD"), transactionType: TransactionType.Deposit);
        var act = async () => await _eventStore.Append(
            userB.Id, userB.GetUncommittedEvents(), userB.CurrentVersion, default);

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

}
