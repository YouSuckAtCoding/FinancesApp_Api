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

public class GetActiveAccountsHandlerTests
{
    private readonly IAccountReadRepository _mockRepository;
    private readonly ILogger<GetActiveAccountsHandler> _mockLogger;
    private readonly GetActiveAccountsHandler _handler;

    public GetActiveAccountsHandlerTests()
    {
        _mockRepository = Substitute.For<IAccountReadRepository>();
        _mockLogger = Substitute.For<ILogger<GetActiveAccountsHandler>>();
        _handler = new GetActiveAccountsHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_Only_Active_Accounts()
    {
        // Arrange
        var expectedAccounts = new List<Account>
        {
            new Account(Guid.NewGuid(), "Active Account 1", new Money(1000, "USD"), AccountType.Checking),
            new Account(Guid.NewGuid(), "Active Account 2", new Money(2000, "USD"), AccountType.CreditCard)
        }.AsReadOnly();

        _mockRepository.GetActiveAccounts(token: Arg.Any<CancellationToken>())
            .Returns(expectedAccounts);

        var query = new GetActiveAccounts();

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedAccounts);
        await _mockRepository.Received(1).GetActiveAccounts(token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Active_Accounts_Exist()
    {
        // Arrange
        _mockRepository.GetActiveAccounts(token: Arg.Any<CancellationToken>())
            .Returns(new List<Account>().AsReadOnly());

        var query = new GetActiveAccounts();

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
        _mockRepository.GetActiveAccounts(token: default)
            .Throws(new Exception("Database error"));

        var query = new GetActiveAccounts();

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Repository()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _mockRepository.GetActiveAccounts(token: cancellationToken)
            .Returns(new List<Account>().AsReadOnly());

        var query = new GetActiveAccounts();

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        await _mockRepository.Received(1).GetActiveAccounts(token: cancellationToken);
    }

    [Fact]
    public async Task Should_Not_Return_Closed_Accounts()
    {
        // Arrange
        var activeAccounts = new List<Account>
        {
            new Account(Guid.NewGuid(), "Active Account", new Money(1000, "USD"), AccountType.Checking)
        }.AsReadOnly();

        _mockRepository.GetActiveAccounts(token: Arg.Any<CancellationToken>())
            .Returns(activeAccounts);

        var query = new GetActiveAccounts();

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().OnlyContain(a => a.Status == AccountStatus.Active);
    }
}

