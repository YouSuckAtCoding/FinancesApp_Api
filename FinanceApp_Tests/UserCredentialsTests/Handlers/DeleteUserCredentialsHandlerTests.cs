using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Commands.Handlers;
using FinancesApp_Module_Credentials.Application.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.UserCredentialsTests.Handlers;

public class DeleteUserCredentialsHandlerTests
{
    private readonly IUserCredentialsRepository _mockRepository;
    private readonly ILogger<DeleteUserCredentialsHandler> _mockLogger;
    private readonly DeleteUserCredentialsHandler _handler;

    public DeleteUserCredentialsHandlerTests()
    {
        _mockRepository = Substitute.For<IUserCredentialsRepository>();
        _mockLogger = Substitute.For<ILogger<DeleteUserCredentialsHandler>>();
        _handler = new DeleteUserCredentialsHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_True_When_Credentials_Deleted_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockRepository.DeleteUserCredentialsAsync(userId, token: Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new DeleteUserCredentials(userId);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().BeTrue();
        await _mockRepository.Received(1).DeleteUserCredentialsAsync(userId, token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_False_When_User_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockRepository.DeleteUserCredentialsAsync(userId, token: Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new DeleteUserCredentials (userId );

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Return_False_When_Exception_Occurs()
    {

        var userId = Guid.NewGuid();

        _mockRepository.DeleteUserCredentialsAsync(userId, token: Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed"));

        var command = new DeleteUserCredentials(userId);

        var result = await _handler.Handle(command);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Repository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        _mockRepository.DeleteUserCredentialsAsync(userId, token: cancellationToken)
            .Returns(true);

        var command = new DeleteUserCredentials(userId);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        await _mockRepository.Received(1).DeleteUserCredentialsAsync(userId, token: cancellationToken);
    }
}