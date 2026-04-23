using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Queries;
using FinancesApp_Module_Account.Application.Queries.Handlers;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.Unit.AccountTests.Handlers;

public class GetAccountByIdHandlerTests
{
    private readonly IEventStore _mockEventStore;
    private readonly ILogger<GetAccountByIdHandler> _mockLogger;
    private readonly GetAccountByIdHandler _handler;

    public GetAccountByIdHandlerTests()
    {
        _mockEventStore = Substitute.For<IEventStore>();
        _mockLogger = Substitute.For<ILogger<GetAccountByIdHandler>>();
        _handler = new GetAccountByIdHandler(_mockLogger, _mockEventStore);
    }

    [Fact]
    public async Task Should_Return_Account_When_Found()
    {
        var expectedAccount = new Account(Guid.NewGuid(), new Money(1000, "USD"), AccountType.Checking);
        var accountId = expectedAccount.Id;
        var events = expectedAccount.GetUncommittedEvents().ToList();

        _mockEventStore.Load(accountId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(events);

        var query = new GetAccountById { AccountId = accountId };
        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Id.Should().Be(accountId);
        await _mockEventStore.Received(1).Load(accountId, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_Account_When_Exception_Occurs()
    {
        var accountId = Guid.NewGuid();
        _mockEventStore.Load(accountId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        var query = new GetAccountById { AccountId = accountId };
        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new Account());
    }
}
