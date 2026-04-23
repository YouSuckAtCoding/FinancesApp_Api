// GetUserByIdHandlerTests.cs
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Queries;
using FinancesApp_Module_User.Application.Queries.Handlers;
using FinancesApp_Module_User.Domain;
using FinancesApp_Module_User.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.Unit.UserTests.Handlers;

public class GetUserByIdHandlerTests
{
    private readonly IEventStore _mockEventStore;
    private readonly ILogger<GetUserByIdHandler> _mockLogger;
    private readonly GetUserByIdHandler _handler;

    public GetUserByIdHandlerTests()
    {
        _mockEventStore = Substitute.For<IEventStore>();
        _mockLogger = Substitute.For<ILogger<GetUserByIdHandler>>();
        _handler = new GetUserByIdHandler(_mockEventStore, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_User_When_Found()
    {
        var userId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new UserCreatedEvent(Guid.NewGuid(),
                                DateTimeOffset.UtcNow,
                                userId,
                                "John Doe",
                                "john.doe@email.com",
                                DateTimeOffset.UtcNow.AddYears(-30),
                                "profile.jpg",
                                DateTimeOffset.UtcNow)
        };

        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Returns(events);

        var query = new GetUserById { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Name.Should().Be("John Doe");
        result.Email.Should().Be("john.doe@email.com");
        await _mockEventStore.Received(1).Load(userId, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_User_When_Exception_Occurs()
    {
        var userId = Guid.NewGuid();
        _mockEventStore.Load(userId, 0, Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        var query = new GetUserById { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new User());
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_EventStore()
    {
        var userId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();
        _mockEventStore.Load(userId, 0, cancellationToken)
            .Returns(new List<IDomainEvent>());

        var query = new GetUserById { UserId = userId };

        await _handler.Handle(query, cancellationToken);

        await _mockEventStore.Received(1).Load(userId, 0, cancellationToken);
    }
}
