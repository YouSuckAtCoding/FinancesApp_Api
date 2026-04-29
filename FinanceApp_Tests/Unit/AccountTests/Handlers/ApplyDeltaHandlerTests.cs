using FinancesApp_CQRS.EventStore;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Commands;
using FinancesApp_Module_Account.Application.Commands.Handlers;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.Events;
using FinancesApp_Module_Account.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinancesApp_Tests.Unit.AccountTests.Handlers;

public class ApplyDeltaHandlerTests
{
    private readonly Mock<IAccountRepository> _repoMock;
    private readonly Mock<IEventStore> _mockStore;
    private readonly Mock<ILogger<CreateAccountHandler>> _loggerMock;
    private readonly ApplyDeltaHandler _handler;

    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Money DefaultBalance = new(1000m, "USD");

    public ApplyDeltaHandlerTests()
    {
        _repoMock = new Mock<IAccountRepository>();
        _loggerMock = new Mock<ILogger<CreateAccountHandler>>();
        _mockStore = new Mock<IEventStore>();
        _handler = new ApplyDeltaHandler(_loggerMock.Object, _repoMock.Object, _mockStore.Object);
    }

    private static Account BuildCheckingAccount() =>
        new(UserId, DefaultBalance, AccountType.Checking);

    private static Account BuildCreditCardAccount() =>
        new(UserId, new Money(0m, "USD"), AccountType.CreditCard);

    private static ApplyDelta BuildCommand(Account account,
        decimal value = 100m,
        string currency = "USD",
        OperationType operationType = OperationType.MoneyTransaction) =>
        new()
        {
            Account = account,
            Delta = new Money(value, currency),
            OperationType = operationType,
            RequestedAt = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenTransactionSucceeds()
    {
        var account = BuildCheckingAccount();
        var command = BuildCommand(account);

        var result = await _handler.Handle(command);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        _mockStore.Verify(r => r.Append(account.Id, It.IsAny<IReadOnlyList<IDomainEvent>>(), account.CurrentVersion, It.IsAny<CancellationToken>()),
                          Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_ForCreditCardPurchase()
    {
        var account = BuildCreditCardAccount();
        var command = BuildCommand(account, value: 200m, operationType: OperationType.CreditPurchase);

        var result = await _handler.Handle(command);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_ForCreditCardPayment()
    {
        var account = new Account(AccountId, UserId,
            new Money(0m, "USD"), new Money(4500m, "USD"),
            new Money(300m, "USD"), AccountStatus.Active,
            AccountType.CreditCard, null, null, DateTimeOffset.UtcNow, null);

        var command = BuildCommand(account, value: 100m, operationType: OperationType.Payment);

        var result = await _handler.Handle(command);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Handle_ShouldFail_AndPersistError_WhenAccountIsClosed()
    {
        var account = new Account(AccountId, UserId,
            new Money(0m, "USD"), new Money(0m, "USD"),
            new Money(0m, "USD"), AccountStatus.Closed,
            AccountType.Checking, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var command = BuildCommand(account);

        var result = await _handler.Handle(command);

        Assert.False(result.Success);
        Assert.Equal("Account is closed.", result.ErrorMessage);
        _mockStore.Verify(r => r.Append(account.Id,
                                        It.Is<IReadOnlyList<IDomainEvent>>(evts => evts.OfType<ApplyDeltaErrorEvent>().Any()),
                                        It.IsAny<int>(),
                                        It.IsAny<CancellationToken>()),
                          Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCurrencyMismatches()
    {
        var account = BuildCheckingAccount();
        var command = BuildCommand(account, currency: "BRL");

        var result = await _handler.Handle(command);

        Assert.False(result.Success);
        Assert.Equal("Currency mismatch.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCashAccountHasInsufficientFunds()
    {
        var account = new Account(UserId,
            new Money(50m, "USD"), AccountType.Cash);

        var command = BuildCommand(account, value: 200m);

        var result = await _handler.Handle(command);

        Assert.False(result.Success);
        Assert.Equal("Insufficient funds in cash account.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCreditLimitExceeded()
    {
        var account = BuildCreditCardAccount();
        var command = BuildCommand(account, value: 5000m, operationType: OperationType.CreditPurchase);

        var result = await _handler.Handle(command);

        Assert.False(result.Success);
        Assert.Equal("Credit limit exceeded.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCreditCardAccountUsesMoneyTransaction()
    {
        var account = BuildCreditCardAccount();
        var command = BuildCommand(account, value: 100m, operationType: OperationType.MoneyTransaction);

        var result = await _handler.Handle(command);

        Assert.False(result.Success);
        Assert.Equal("Credit card accounts only support credit card operations.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRequestedAt_IsTooFarInPast()
    {
        var account = BuildCheckingAccount();
        var command = BuildCommand(account);
        command.RequestedAt = DateTimeOffset.UtcNow - ApplyDeltaHandler.RequestedAtSkewTolerance - TimeSpan.FromSeconds(1);

        var result = await _handler.Handle(command);

        Assert.False(result.Success);
        Assert.Equal("RequestedAt is outside the allowed time window.", result.ErrorMessage);

        _mockStore.Verify(r => r.Append(account.Id, It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                          Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRequestedAt_IsTooFarInFuture()
    {
        var account = BuildCheckingAccount();
        var command = BuildCommand(account);
        command.RequestedAt = DateTimeOffset.UtcNow + ApplyDeltaHandler.RequestedAtSkewTolerance + TimeSpan.FromSeconds(1);

        var result = await _handler.Handle(command);

        Assert.False(result.Success);
        Assert.Equal("RequestedAt is outside the allowed time window.", result.ErrorMessage);

        _mockStore.Verify(r => r.Append(account.Id, It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                          Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenRequestedAt_IsWithinSkewTolerance_Past()
    {
        var account = BuildCheckingAccount();
        var command = BuildCommand(account);
        command.RequestedAt = DateTimeOffset.UtcNow - ApplyDeltaHandler.RequestedAtSkewTolerance + TimeSpan.FromSeconds(5);

        var result = await _handler.Handle(command);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenRequestedAt_IsWithinSkewTolerance_Future()
    {
        var account = BuildCheckingAccount();
        var command = BuildCommand(account);
        command.RequestedAt = DateTimeOffset.UtcNow + ApplyDeltaHandler.RequestedAtSkewTolerance - TimeSpan.FromSeconds(5);

        var result = await _handler.Handle(command);

        Assert.True(result.Success);
    }
}
