using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Queries;
using FinancesApp_Module_Credentials.Application.Queries.Handlers;
using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_Credentials.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.Unit.UserCredentialsTests.Handlers;

public class GetUserCredentialsByUserIdHandlerTests
{
    private readonly IEventStore _mockEventStore;
    private readonly ILogger<GetUserCredentialsByUserIdHandler> _mockLogger;
    private readonly GetUserCredentialsByUserIdHandler _handler;

    public GetUserCredentialsByUserIdHandlerTests()
    {
        _mockEventStore = Substitute.For<IEventStore>();
        _mockLogger = Substitute.For<ILogger<GetUserCredentialsByUserIdHandler>>();
        _handler = new GetUserCredentialsByUserIdHandler(_mockEventStore, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_Credentials_When_Found()
    {
        var userId = Guid.NewGuid();
        var credentialsId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new CredentialsRegisteredEvent(Guid.NewGuid(), DateTimeOffset.UtcNow,
                credentialsId, userId, "john_doe", "$2a$11$hashedpassword")
        };

        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Returns(events);

        var query = new GetUserCredentialsByUserId { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Id.Should().Be(credentialsId);
        result.UserId.Should().Be(userId);
        result.Email.Should().Be("john_doe");
        await _mockEventStore.Received(1).Load(userId, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_Credentials_When_Exception_Occurs()
    {
        var userId = Guid.NewGuid();

        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed"));

        var query = new GetUserCredentialsByUserId { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new UserCredentials());
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_EventStore()
    {
        var userId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();
        _mockEventStore.Load(userId, 0, cancellationToken)
            .Returns(new List<IDomainEvent>());

        var query = new GetUserCredentialsByUserId { UserId = userId };

        await _handler.Handle(query, cancellationToken);

        await _mockEventStore.Received(1).Load(userId, 0, cancellationToken);
    }
}
