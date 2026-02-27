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

public class GetAccountByIdHandlerTests
{
    private readonly IAccountReadRepository _mockRepository;
    private readonly ILogger<GetAccountByIdHandler> _mockLogger;
    private readonly GetAccountByIdHandler _handler;

    public GetAccountByIdHandlerTests()
    {
        _mockRepository = Substitute.For<IAccountReadRepository>();
        _mockLogger = Substitute.For<ILogger<GetAccountByIdHandler>>();
        _handler = new GetAccountByIdHandler(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_Account_When_Found()
    {
        var accountId = Guid.NewGuid();
        var expectedAccount = new Account(
            accountId,
            new Guid(),
            "Test Account",
            new Money(1000, "USD"),
            AccountType.Checking
        );

        _mockRepository.GetAccountById(accountId, token: Arg.Any<CancellationToken>())
            .Returns(expectedAccount);

        var query = new GetAccountById { AccountId = accountId };

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Id.Should().Be(accountId);
        result.Name.Should().Be("Test Account");
        await _mockRepository.Received(1).GetAccountById(accountId, token: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Empty_Account_When_Exception_Occurs()
    {
        var accountId = Guid.NewGuid();
        _mockRepository.GetAccountById(accountId, token: Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        var query = new GetAccountById { AccountId = accountId };
        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new Account());
    }


}