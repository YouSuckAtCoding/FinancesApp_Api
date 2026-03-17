using FinancesApp_CQRS.EventStore;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;

namespace FinancesApp_Tests.AccountTests;
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

        

        //rebuiltAccount.Id.Should().Be(account.Id);
        //rebuiltAccount.Balance.Should().Be(account.Balance);
        //rebuiltAccount.Type.Should().Be(account.Type);
        //rebuiltAccount.CurrentVersion.Should().Be(account.NextVersion);
    }


}
