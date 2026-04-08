using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Commands.Handlers;
using FinancesApp_Module_Credentials.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.UserCredentialsTests.Handlers;

public class UpdateUserCredentialsHandlerTests
{
    private readonly IEventStore _mockEventStore;
    private readonly ILogger<UpdateUserCredentialsHandler> _mockLogger;
    private readonly UpdateUserCredentialsHandler _handler;

    public UpdateUserCredentialsHandlerTests()
    {
        _mockEventStore = Substitute.For<IEventStore>();
        _mockLogger = Substitute.For<ILogger<UpdateUserCredentialsHandler>>();
        _handler = new UpdateUserCredentialsHandler(_mockEventStore, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_True_When_Password_Updated_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialsId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new CredentialsRegisteredEvent(Guid.NewGuid(), DateTimeOffset.UtcNow,
                credentialsId, userId, "john_doe", "$2a$11$hashedpassword")
        };

        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Returns(events);

        var command = new UpdateUserCredentials(userId, "NewPassword123!");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().BeTrue();
        await _mockEventStore.Received(1).Append(
            userId,
            Arg.Any<IReadOnlyList<IDomainEvent>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_False_When_Exception_Occurs()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed"));

        var command = new UpdateUserCredentials(userId, "NewPassword123!");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_EventStore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();
        var events = new List<IDomainEvent>
        {
            new CredentialsRegisteredEvent(Guid.NewGuid(), DateTimeOffset.UtcNow,
                Guid.NewGuid(), userId, "john_doe", "$2a$11$hashedpassword")
        };

        _mockEventStore.Load(userId, 0, cancellationToken)
            .Returns(events);

        var command = new UpdateUserCredentials(userId, "NewPassword123!");

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        await _mockEventStore.Received(1).Append(
            userId,
            Arg.Any<IReadOnlyList<IDomainEvent>>(),
            Arg.Any<int>(),
            cancellationToken);
    }
}
