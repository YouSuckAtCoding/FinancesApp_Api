using FinancesApp_Module_User.Application.Queries;
using FinancesApp_Module_User.Application.Queries.Handlers;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.UserTests.Handlers;

public class GetUserByEmailHandlerTests
{
    private readonly IUserReadRepository _mockRepository;
    private readonly ILogger<GetUserByEmailHandler> _mockLogger;
    private readonly GetUserByEmailHandler _handler;

    public GetUserByEmailHandlerTests()
    {
        _mockRepository = Substitute.For<IUserReadRepository>();
        _mockLogger = Substitute.For<ILogger<GetUserByEmailHandler>>();
        _handler = new GetUserByEmailHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_User_When_Found()
    {
        // Arrange
        var email = "john.doe@email.com";
        var expected = new User(Guid.NewGuid(), "John Doe", email,
            DateTimeOffset.UtcNow.AddYears(-25), "profile.jpg");

        _mockRepository.GetUserByEmail(email, token: Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetUserByEmail { Email = email };

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expected);
        await _mockRepository.Received(1).GetUserByEmail(email, token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_User_When_Not_Found()
    {
        // Arrange
        var email = "notfound@email.com";

        _mockRepository.GetUserByEmail(email, token: Arg.Any<CancellationToken>())
            .Returns(new User());

        var query = new GetUserByEmail { Email = email };

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new User());
    }

    [Fact]
    public async Task Should_Return_Empty_User_When_Exception_Occurs()
    {
        // Arrange
        var email = "john.doe@email.com";

        _mockRepository.GetUserByEmail(email, token: Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed"));

        var query = new GetUserByEmail { Email = email };

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new User());
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Repository()
    {
        // Arrange
        var email = "john.doe@email.com";
        var cancellationToken = new CancellationToken();

        _mockRepository.GetUserByEmail(email, token: cancellationToken)
            .Returns(new User());

        var query = new GetUserByEmail { Email = email };

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        await _mockRepository.Received(1).GetUserByEmail(email, token: cancellationToken);
    }
}