using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Commands.Handlers;
using FinancesApp_Module_Credentials.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.Unit.UserCredentialsTests.Handlers;

public class TotpCredentialCreatedHandlerTests
{
    private readonly IEventStore _mockEventStore;
    private readonly ILogger<TotpCredentialCreatedHandler> _mockLogger;
    private readonly TotpCredentialCreatedHandler _handler;

    public TotpCredentialCreatedHandlerTests()
    {
        _mockEventStore = Substitute.For<IEventStore>();
        _mockLogger = Substitute.For<ILogger<TotpCredentialCreatedHandler>>();
        _handler = new TotpCredentialCreatedHandler(_mockEventStore, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_True_When_Successful()
    {
        var totp = new UserCredentialsTotp(Guid.NewGuid(), "JBSWY3DPEHPK3PXP");
        var command = new TotpCredentialCreated(totp);

        var result = await _handler.Handle(command);

        result.Should().BeTrue();
        await _mockEventStore.Received(1).Append(
            totp.Id,
            Arg.Any<IReadOnlyList<IDomainEvent>>(),
            totp.CurrentVersion,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_False_When_Exception_Occurs()
    {
        var totp = new UserCredentialsTotp(Guid.NewGuid(), "JBSWY3DPEHPK3PXP");

        _mockEventStore.Append(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyList<IDomainEvent>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Throws(new Exception("Database failure"));

        var command = new TotpCredentialCreated(totp);

        var result = await _handler.Handle(command);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_EventStore()
    {
        var totp = new UserCredentialsTotp(Guid.NewGuid(), "JBSWY3DPEHPK3PXP");
        var cts = new CancellationToken();
        var command = new TotpCredentialCreated(totp);

        await _handler.Handle(command, cts);

        await _mockEventStore.Received(1).Append(
            totp.Id,
            Arg.Any<IReadOnlyList<IDomainEvent>>(),
            totp.CurrentVersion,
            cts);
    }
}
