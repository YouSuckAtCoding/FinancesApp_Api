using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Commands.Handlers;
using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_Credentials.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.Unit.UserCredentialsTests.Handlers;

public class InvalidateTotpCredentialHandlerTests
{
    private readonly IEventStore _mockEventStore;
    private readonly ILogger<InvalidateTotpCredentialHandler> _mockLogger;
    private readonly InvalidateTotpCredentialHandler _handler;

    public InvalidateTotpCredentialHandlerTests()
    {
        _mockEventStore = Substitute.For<IEventStore>();
        _mockLogger = Substitute.For<ILogger<InvalidateTotpCredentialHandler>>();
        _handler = new InvalidateTotpCredentialHandler(_mockEventStore, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_True_When_Active_Totp_Invalidated()
    {
        var userId = Guid.NewGuid();
        var createdEvent = new TotpCredentialCreatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), userId, "JBSWY3DPEHPK3PXP");

        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Returns(new List<IDomainEvent> { createdEvent });

        var command = new InvalidateTotpCredential(userId);

        var result = await _handler.Handle(command);

        result.Should().BeTrue();
        await _mockEventStore.Received(1).Append(
            userId,
            Arg.Any<IReadOnlyList<IDomainEvent>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_False_When_No_Events_Exist()
    {
        var userId = Guid.NewGuid();

        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Returns(new List<IDomainEvent>());

        var command = new InvalidateTotpCredential(userId);

        var result = await _handler.Handle(command);

        result.Should().BeFalse();
        await _mockEventStore.DidNotReceive().Append(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyList<IDomainEvent>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_False_When_Already_Invalidated()
    {
        var userId = Guid.NewGuid();
        var createdEvent = new TotpCredentialCreatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), userId, "JBSWY3DPEHPK3PXP");
        var invalidatedEvent = new TotpCredentialInvalidatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), userId);

        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Returns(new List<IDomainEvent> { createdEvent, invalidatedEvent });

        var command = new InvalidateTotpCredential(userId);

        var result = await _handler.Handle(command);

        result.Should().BeFalse();
        await _mockEventStore.DidNotReceive().Append(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyList<IDomainEvent>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_False_When_Exception_Occurs()
    {
        var userId = Guid.NewGuid();

        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Throws(new Exception("Database failure"));

        var command = new InvalidateTotpCredential(userId);

        var result = await _handler.Handle(command);

        result.Should().BeFalse();
    }
}
