using FinancesApp_CQRS.Queries;
using FinancesApp_Module_Account.Application.Queries;
using FinancesApp_Module_Account.Application.Queries.Handlers;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.AccountTests.Handlers;
public class GetAccountsHandlerTests
{
    private readonly IAccountReadRepository _mockRepository;
    private readonly ILogger<GetAccountsHandler> _mockLogger;
    private readonly GetAccountsHandler _handler;

    public GetAccountsHandlerTests()
    {
        _mockRepository = Substitute.For<IAccountReadRepository>();
        _mockLogger = Substitute.For<ILogger<GetAccountsHandler>>();
        _handler = new GetAccountsHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_All_Accounts_When_Successful()
    {
        // Arrange
        var expectedAccounts = new List<Account>
        {
            new Account(Guid.NewGuid(), "Account 1", new Money(1000, "USD"), AccountType.Checking),
            new Account(Guid.NewGuid(), "Account 2", new Money(2000, "USD"), AccountType.Cash),
            new Account(Guid.NewGuid(), "Account 3", new Money(3000, "USD"), AccountType.CreditCard)
        }.AsReadOnly();

        _mockRepository.GetAccounts(token: Arg.Any<CancellationToken>())
            .Returns(expectedAccounts);

        var query = new GetAccounts();

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedAccounts);
        await _mockRepository.Received(1).GetAccounts(token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Accounts_Exist()
    {
        // Arrange
        _mockRepository.GetAccounts(token: default)
            .Returns(new List<Account>().AsReadOnly());

        var query = new GetAccounts();

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_Exception_Occurs()
    {
        // Arrange
        _mockRepository.GetAccounts(token: default)
            .Throws(new Exception("Database connection failed"));

        var query = new GetAccounts();

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();        
    }

}

