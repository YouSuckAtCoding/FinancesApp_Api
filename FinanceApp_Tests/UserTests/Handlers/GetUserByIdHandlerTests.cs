// GetUserByIdHandlerTests.cs
using FinancesApp_Module_User.Application.Queries;
using FinancesApp_Module_User.Application.Queries.Handlers;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.UserTests.Handlers;

public class GetUserByIdHandlerTests
{
    private readonly IUserReadRepository _mockRepository;
    private readonly ILogger<GetUserByIdHandler> _mockLogger;
    private readonly GetUserByIdHandler _handler;

    public GetUserByIdHandlerTests()
    {
        _mockRepository = Substitute.For<IUserReadRepository>();
        _mockLogger = Substitute.For<ILogger<GetUserByIdHandler>>();
        _handler = new GetUserByIdHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_User_When_Found()
    {
        
        var userId = Guid.NewGuid();
        var expectedUser = new User(
            userId,
            "John Doe",
            "john.doe@email.com",
            DateTimeOffset.UtcNow.AddYears(-30),
            "profile.jpg"
        );

        _mockRepository.GetUserById(userId, token: Arg.Any<CancellationToken>())
            .Returns(expectedUser);

        var query = new GetUserById { UserId = userId };
        
        var result = await _handler.Handle(query);
        
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        await _mockRepository.Received(1).GetUserById(userId, token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_User_When_Exception_Occurs()
    {
        
        var userId = Guid.NewGuid();
        _mockRepository.GetUserById(userId, token: Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        var query = new GetUserById { UserId = userId };
        
        var result = await _handler.Handle(query);
        
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new User());
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Repository()
    {
        var userId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();
        _mockRepository.GetUserById(userId, token: cancellationToken)
            .Returns(new User());

        var query = new GetUserById { UserId = userId };
   
        await _handler.Handle(query, cancellationToken);
        
        await _mockRepository.Received(1).GetUserById(userId, token: cancellationToken);
    }
}