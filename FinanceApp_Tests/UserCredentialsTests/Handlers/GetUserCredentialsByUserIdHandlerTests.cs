using FinancesApp_Module_Credentials.Application.Queries;
using FinancesApp_Module_Credentials.Application.Queries.Handlers;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.UserCredentialsTests.Handlers;

public class GetUserCredentialsByUserIdHandlerTests
{
    private readonly IUserCredentialsReadRepository _mockRepository;
    private readonly ILogger<GetUserCredentialsByUserIdHandler> _mockLogger;
    private readonly GetUserCredentialsByUserIdHandler _handler;

    public GetUserCredentialsByUserIdHandlerTests()
    {
        _mockRepository = Substitute.For<IUserCredentialsReadRepository>();
        _mockLogger = Substitute.For<ILogger<GetUserCredentialsByUserIdHandler>>();
        _handler = new GetUserCredentialsByUserIdHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_Credentials_When_Found()
    {
        var userId = Guid.NewGuid();
        var expected = new UserCredentials(Guid.NewGuid(), userId, "john_doe", "$2a$11$hashedpassword");

        _mockRepository.GetByUserIdAsync(userId, token: Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetUserCredentialsByUserId { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expected);
        await _mockRepository.Received(1).GetByUserIdAsync(userId, token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_Credentials_When_Not_Found()
    {
        var userId = Guid.NewGuid();

        _mockRepository.GetByUserIdAsync(userId, token: Arg.Any<CancellationToken>())
            .Returns(new UserCredentials());

        var query = new GetUserCredentialsByUserId { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new UserCredentials());
    }

    [Fact]
    public async Task Should_Return_Empty_Credentials_When_Exception_Occurs()
    {
        var userId = Guid.NewGuid();

        _mockRepository.GetByUserIdAsync(userId, token: Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed"));

        var query = new GetUserCredentialsByUserId { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new UserCredentials());
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Repository()
    {
        var userId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        _mockRepository.GetByUserIdAsync(userId, token: cancellationToken)
            .Returns(new UserCredentials());

        var query = new GetUserCredentialsByUserId { UserId = userId };

        await _handler.Handle(query, cancellationToken);

        await _mockRepository.Received(1).GetByUserIdAsync(userId, token: cancellationToken);
    }
}