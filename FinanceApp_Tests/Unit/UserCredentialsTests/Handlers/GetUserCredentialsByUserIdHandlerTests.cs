using FinancesApp_Module_Credentials.Application.Queries;
using FinancesApp_Module_Credentials.Application.Queries.Handlers;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.Unit.UserCredentialsTests.Handlers;

public class GetUserCredentialsByUserIdHandlerTests
{
    private readonly IUserCredentialsReadRepository _mockReadRepository;
    private readonly ILogger<GetUserCredentialsByUserIdHandler> _mockLogger;
    private readonly GetUserCredentialsByUserIdHandler _handler;

    public GetUserCredentialsByUserIdHandlerTests()
    {
        _mockReadRepository = Substitute.For<IUserCredentialsReadRepository>();
        _mockLogger = Substitute.For<ILogger<GetUserCredentialsByUserIdHandler>>();
        _handler = new GetUserCredentialsByUserIdHandler(_mockReadRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_Credentials_When_Found()
    {
        var userId = Guid.NewGuid();
        var credentialsId = Guid.NewGuid();
        var credentials = new UserCredentials(credentialsId, userId, "john_doe", "$2a$11$hashedpassword");

        _mockReadRepository.GetByUserIdAsync(userId, Arg.Any<SqlConnection?>(), Arg.Any<CancellationToken>())
            .Returns(credentials);

        var query = new GetUserCredentialsByUserId { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Id.Should().Be(credentialsId);
        result.UserId.Should().Be(userId);
        result.Email.Should().Be("john_doe");
        await _mockReadRepository.Received(1).GetByUserIdAsync(userId, Arg.Any<SqlConnection?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_Credentials_When_Exception_Occurs()
    {
        var userId = Guid.NewGuid();

        _mockReadRepository.GetByUserIdAsync(userId, Arg.Any<SqlConnection?>(), Arg.Any<CancellationToken>())
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
        _mockReadRepository.GetByUserIdAsync(userId, Arg.Any<SqlConnection?>(), cancellationToken)
            .Returns(new UserCredentials());

        var query = new GetUserCredentialsByUserId { UserId = userId };

        await _handler.Handle(query, cancellationToken);

        await _mockReadRepository.Received(1).GetByUserIdAsync(userId, Arg.Any<SqlConnection?>(), cancellationToken);
    }
}
