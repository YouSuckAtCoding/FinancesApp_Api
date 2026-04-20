using FinanceAppDatabase.DbConnection;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Commands.Handlers;
using FinancesApp_Module_Credentials.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.UserCredentialsTests.Handlers;

public class RebuildCredentialsProjectionHandlerTests
{
    private readonly IEventStore _mockEventStore;
    private readonly IEventDispatcher _mockDispatcher;
    private readonly IDbConnectionFactory _mockConnectionFactory;
    private readonly ILogger<RebuildCredentialsProjectionHandler> _mockLogger;
    private readonly RebuildCredentialsProjectionHandler _handler;

    public RebuildCredentialsProjectionHandlerTests()
    {
        _mockEventStore = Substitute.For<IEventStore>();
        _mockDispatcher = Substitute.For<IEventDispatcher>();
        _mockConnectionFactory = Substitute.For<IDbConnectionFactory>();
        _mockLogger = Substitute.For<ILogger<RebuildCredentialsProjectionHandler>>();
        _handler = new RebuildCredentialsProjectionHandler(
            _mockEventStore, _mockDispatcher, _mockConnectionFactory, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_False_When_No_Events_Exist()
    {
        var userId = Guid.NewGuid();
        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Returns(new List<IDomainEvent>());

        var result = await _handler.Handle(new RebuildCredentialsProjection(userId));

        result.Should().BeFalse();
        await _mockDispatcher.DidNotReceive().Dispatch(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Load_Events_And_Dispatch_Each_One()
    {
        var userId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new CredentialsRegisteredEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), userId, "user@test.com", "hashedpw"),
            new CredentialsPasswordChangedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), userId, "newhash")
        };

        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Returns(events);

        var result = await _handler.Handle(new RebuildCredentialsProjection(userId));

        result.Should().BeTrue();
        await _mockDispatcher.Received(2).Dispatch(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        await _mockDispatcher.Received(1).Dispatch(Arg.Is<IDomainEvent>(e => e is CredentialsRegisteredEvent), Arg.Any<CancellationToken>());
        await _mockDispatcher.Received(1).Dispatch(Arg.Is<IDomainEvent>(e => e is CredentialsPasswordChangedEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_False_When_EventStore_Throws()
    {
        var userId = Guid.NewGuid();
        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Throws(new Exception("DB down"));

        var result = await _handler.Handle(new RebuildCredentialsProjection(userId));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_EventStore()
    {
        var userId = Guid.NewGuid();
        var cts = new CancellationToken();
        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Returns(new List<IDomainEvent>());

        await _handler.Handle(new RebuildCredentialsProjection(userId), cts);

        await _mockEventStore.Received(1).Load(userId, 0, cts);
    }
}
