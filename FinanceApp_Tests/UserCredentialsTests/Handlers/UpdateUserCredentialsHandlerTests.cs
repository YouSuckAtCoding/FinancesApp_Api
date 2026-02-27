using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Commands.Handlers;
using FinancesApp_Module_Credentials.Application.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.UserCredentialsTests.Handlers;

public class UpdateUserCredentialsHandlerTests
{
    private readonly IUserCredentialsRepository _mockRepository;
    private readonly ILogger<UpdateUserCredentialsHandler> _mockLogger;
    private readonly UpdateUserCredentialsHandler _handler;

    public UpdateUserCredentialsHandlerTests()
    {
        _mockRepository = Substitute.For<IUserCredentialsRepository>();
        _mockLogger = Substitute.For<ILogger<UpdateUserCredentialsHandler>>();
        _handler = new UpdateUserCredentialsHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_True_When_Password_Updated_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockRepository.UpdatePasswordAsync(userId, Arg.Any<string>(), token: Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new UpdateUserCredentials(userId, "NewPassword123!");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().BeTrue();
        await _mockRepository.Received(1).UpdatePasswordAsync(userId, Arg.Any<string>(), token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_False_When_User_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockRepository.UpdatePasswordAsync(userId, Arg.Any<string>(), token: Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new UpdateUserCredentials(userId, "NewPassword123!");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Return_False_When_Exception_Occurs()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockRepository.UpdatePasswordAsync(userId, Arg.Any<string>(), token: Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed"));

        var command = new UpdateUserCredentials ( userId, "NewPassword123!" );

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Repository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        _mockRepository.UpdatePasswordAsync(userId, Arg.Any<string>(), token: cancellationToken)
            .Returns(true);

        var command = new UpdateUserCredentials(userId, "NewPassword123!");

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        await _mockRepository.Received(1).UpdatePasswordAsync(userId, Arg.Any<string>(), token: cancellationToken);
    }
}