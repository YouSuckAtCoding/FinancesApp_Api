using FinancesApp_CQRS.EventStore;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Commands;
using FinancesApp_Module_Account.Application.Commands.Handlers;
using FinancesApp_Module_Account.Domain;
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
            Value = value,
            Currency = currency,
            OperationType = operationType,
            RequestedAt = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenTransactionSucceeds()
    {
        var account = BuildCheckingAccount();
        var command = BuildCommand(account);

        _mockStore.Setup(r => r.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion,It.IsAny<CancellationToken>()));

        var result = await _handler.Handle(command);

        Assert.True(result);
        _mockStore.Verify(r => r.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion, It.IsAny<CancellationToken>()),
                          Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_ForCreditCardPurchase()
    {
        var account = BuildCreditCardAccount();
        var command = BuildCommand(account, value: 200m, operationType: OperationType.CreditPurchase);

        _repoMock.Setup(r => r.UpdateAccountAsync(account, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var result = await _handler.Handle(command);

        Assert.True(result);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_ForCreditCardPayment()
    {
        var account = new Account(AccountId, UserId,
            new Money(0m, "USD"), new Money(4500m, "USD"),
            new Money(300m, "USD"), AccountStatus.Active,
            AccountType.CreditCard, null, null, DateTimeOffset.UtcNow, null);

        var command = BuildCommand(account, value: 100m, operationType: OperationType.Payment);

        _repoMock.Setup(r => r.UpdateAccountAsync(account, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var result = await _handler.Handle(command);

        Assert.True(result);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenAccountIsClosed()
    {
        var account = new Account(AccountId, UserId,
            new Money(0m, "USD"), new Money(0m, "USD"),
            new Money(0m, "USD"), AccountStatus.Closed,
            AccountType.Checking, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var command = BuildCommand(account);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command));
        _mockStore.Verify(r => r.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion, It.IsAny<CancellationToken>()),
                          Times.Never);
        
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCurrencyMismatches()
    {
        var account = BuildCheckingAccount();
        var command = BuildCommand(account, currency: "BRL");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command));

        _mockStore.Verify(r => r.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion, It.IsAny<CancellationToken>()),
                         Times.Never);

    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCashAccountHasInsufficientFunds()
    {
        var account = new Account(UserId,
            new Money(50m, "USD"), AccountType.Cash);

        var command = BuildCommand(account, value: 200m);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCreditLimitExceeded()
    {
        var account = BuildCreditCardAccount();
        var command = BuildCommand(account, value: 5000m, operationType: OperationType.CreditPurchase);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command));
        _mockStore.Verify(r => r.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion, It.IsAny<CancellationToken>()),
                    Times.Never);
    }

}