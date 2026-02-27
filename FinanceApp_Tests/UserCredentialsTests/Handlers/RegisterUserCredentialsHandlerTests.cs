using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Commands.Handlers;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.UserCredentialsTests.Handlers;

public class RegisterUserCredentialsHandlerTests
{
    private readonly IUserCredentialsRepository _mockRepository;
    private readonly ILogger<RegisterUserCredentialsHandler> _mockLogger;
    private readonly RegisterUserCredentialsHandler _handler;

    public RegisterUserCredentialsHandlerTests()
    {
        _mockRepository = Substitute.For<IUserCredentialsRepository>();
        _mockLogger = Substitute.For<ILogger<RegisterUserCredentialsHandler>>();
        _handler = new RegisterUserCredentialsHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_Inserted_Id_When_Successful()
    {
        // Arrange
        var credentials = new UserCredentials(Guid.NewGuid(), Guid.NewGuid(), "john_doe", "$2a$11$hashedpassword");
        var expectedId = credentials.Id;

        _mockRepository.CreateUserCredentialsAsync(credentials, token: Arg.Any<CancellationToken>())
            .Returns(expectedId);

        var command =  new RegisterUserCredentials(credentials);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().Be(expectedId);
        await _mockRepository.Received(1).CreateUserCredentialsAsync(credentials, token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Guid_Empty_When_Repository_Returns_Empty()
    {
        // Arrange
        var credentials = new UserCredentials(Guid.NewGuid(), Guid.NewGuid(), "john_doe", "$2a$11$hashedpassword");

        _mockRepository.CreateUserCredentialsAsync(credentials, token: Arg.Any<CancellationToken>())
            .Returns(Guid.Empty);

        var command = new RegisterUserCredentials(credentials);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Should_Return_Guid_Empty_When_Exception_Occurs()
    {
        // Arrange
        var credentials = new UserCredentials(Guid.NewGuid(), Guid.NewGuid(), "john_doe", "$2a$11$hashedpassword");

        _mockRepository.CreateUserCredentialsAsync(credentials, token: Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed"));

        var command = new RegisterUserCredentials(credentials);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Repository()
    {
        // Arrange
        var credentials = new UserCredentials(Guid.NewGuid(), Guid.NewGuid(), "john_doe", "$2a$11$hashedpassword");
        var cancellationToken = new CancellationToken();

        _mockRepository.CreateUserCredentialsAsync(credentials, token: cancellationToken)
            .Returns(credentials.Id);

        var command = new RegisterUserCredentials(credentials);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        await _mockRepository.Received(1).CreateUserCredentialsAsync(credentials, token: cancellationToken);
    }
}