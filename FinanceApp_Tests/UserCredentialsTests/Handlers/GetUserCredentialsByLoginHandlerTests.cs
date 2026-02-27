using FinancesApp_Module_Credentials.Application.Queries;
using FinancesApp_Module_Credentials.Application.Queries.Handlers;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.UserCredentialsTests.Handlers;

public class GetUserCredentialsByLoginHandlerTests
{
    private readonly IUserCredentialsReadRepository _mockRepository;
    private readonly ILogger<GetUserCredentialsByLoginHandler> _mockLogger;
    private readonly GetUserCredentialsByLoginHandler _handler;

    public GetUserCredentialsByLoginHandlerTests()
    {
        _mockRepository = Substitute.For<IUserCredentialsReadRepository>();
        _mockLogger = Substitute.For<ILogger<GetUserCredentialsByLoginHandler>>();
        _handler = new GetUserCredentialsByLoginHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_Credentials_When_Found()
    {
        // Arrange
        var login = "john_doe";
        var expected = new UserCredentials(Guid.NewGuid(), Guid.NewGuid(), login, "$2a$11$hashedpassword");

        _mockRepository.GetByLoginAsync(login, token: Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetUserCredentialsByLogin { Login = login };

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expected);
        await _mockRepository.Received(1).GetByLoginAsync(login, token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_Credentials_When_Not_Found()
    {
        // Arrange
        var login = "unknown_user";

        _mockRepository.GetByLoginAsync(login, token: Arg.Any<CancellationToken>())
            .Returns(new UserCredentials());

        var query = new GetUserCredentialsByLogin { Login = login };

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new UserCredentials());
    }

    [Fact]
    public async Task Should_Return_Empty_Credentials_When_Exception_Occurs()
    {
        // Arrange
        var login = "john_doe";

        _mockRepository.GetByLoginAsync(login, token: Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed"));

        var query = new GetUserCredentialsByLogin { Login = login };

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new UserCredentials());
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Repository()
    {
        // Arrange
        var login = "john_doe";
        var cancellationToken = new CancellationToken();

        _mockRepository.GetByLoginAsync(login, token: cancellationToken)
            .Returns(new UserCredentials());

        var query = new GetUserCredentialsByLogin { Login = login };

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        await _mockRepository.Received(1).GetByLoginAsync(login, token: cancellationToken);
    }
}