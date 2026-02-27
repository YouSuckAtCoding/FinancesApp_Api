using FinancesApp_Module_User.Application.Queries;
using FinancesApp_Module_User.Application.Queries.Handlers;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.UserTests.Handlers;

public class GetUsersHandlerTests
{
    private readonly IUserReadRepository _mockRepository;
    private readonly ILogger<GetUserByIdHandler> _mockLogger;
    private readonly GetUsersHandler _handler;

    public GetUsersHandlerTests()
    {
        _mockRepository = Substitute.For<IUserReadRepository>();
        _mockLogger = Substitute.For<ILogger<GetUserByIdHandler>>();
        _handler = new GetUsersHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_All_Users_When_Successful()
    {
        // Arrange
        var dob = DateTimeOffset.UtcNow.AddYears(-30);
        var expectedUsers = new List<User>
        {
            new(Guid.NewGuid(), "John Doe",  "john@email.com",  dob, "john.jpg"),
            new(Guid.NewGuid(), "Jane Doe",  "jane@email.com",  dob, "jane.jpg"),
            new(Guid.NewGuid(), "Bob Smith", "bob@email.com",   dob, "bob.jpg")
        }.AsReadOnly();

        _mockRepository.GetUsers(token: Arg.Any<CancellationToken>())
            .Returns(expectedUsers);

        var query = new GetUsers();

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedUsers);
        await _mockRepository.Received(1).GetUsers(token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Users_Exist()
    {
        _mockRepository.GetUsers(token: Arg.Any<CancellationToken>())
            .Returns(new List<User>().AsReadOnly());

        var query = new GetUsers();

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_Exception_Occurs()
    {        
        _mockRepository.GetUsers(token: Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed"));

        var query = new GetUsers();

        var result = await _handler.Handle(query);
        
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
  
}