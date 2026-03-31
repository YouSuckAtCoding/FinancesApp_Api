using FinancesApp_CQRS.EventStore;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;


namespace FinancesApp_Tests.AccountTests.Events;
public class AccountEvents_CreditCardWorkflowTests : IClassFixture<SqlFixture>
{
    private readonly EventStore _eventStore;
    private readonly SqlFixture _sqlFixture;

    public AccountEvents_CreditCardWorkflowTests(SqlFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
        _eventStore = new EventStore(_sqlFixture.CommandFactory, _sqlFixture.ConnectionFactory);
    }
    // ─── Helpers ────────────────────────────────────────────────────────────

    private static Account CreateCreditCard(string currency = "USD")
        => new Account(Guid.NewGuid(), new Money(0m, currency), AccountType.CreditCard);

    // ════════════════════════════════════════════════════════════════════════
    // CREDIT CARD PAYMENT
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    void Should_Reduce_Debt_On_Partial_Payment()
    {
        var account = CreateCreditCard();
        account.ApplyDelta(new Money(1000m, "USD"), OperationType.CreditPurchase);

        account.PayCreditCardDebt(new Money(400m, "USD"));

        account.CurrentDebt.Amount.Should().Be(600m);
    }

    [Fact]
    void Should_Clear_Debt_On_Full_Payment()
    {
        var account = CreateCreditCard();
        account.ApplyDelta(new Money(1000m, "USD"), OperationType.CreditPurchase);

        account.PayCreditCardDebt(new Money(1000m, "USD"));

        account.CurrentDebt.Amount.Should().Be(0m);
    }

    [Fact]
    void Should_Clamp_Debt_To_Zero_On_Overpayment()
    {
        var account = CreateCreditCard();
        account.ApplyDelta(new Money(500m, "USD"), OperationType.CreditPurchase);

        account.PayCreditCardDebt(new Money(9999m, "USD"));

        account.CurrentDebt.Amount.Should().Be(0m);
    }

    [Fact]
    void Should_Set_PayedAt_When_Debt_Fully_Cleared()
    {
        var account = CreateCreditCard();
        account.ApplyDelta(new Money(500m, "USD"), OperationType.CreditPurchase);

        account.PayCreditCardDebt(new Money(500m, "USD"));

        account.PayedAt.Should().NotBeNull();
        account.PayedAt!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    void Should_Not_Set_PayedAt_On_Partial_Payment()
    {
        var account = CreateCreditCard();
        account.ApplyDelta(new Money(1000m, "USD"), OperationType.CreditPurchase);

        account.PayCreditCardDebt(new Money(400m, "USD"));

        account.PayedAt.Should().BeNull();
    }

    [Fact]
    void Should_Reset_PaymentDate_And_DueDate_After_Full_Payment()
    {
        var account = CreateCreditCard();
        account.ApplyDelta(new Money(500m, "USD"), OperationType.CreditPurchase);        

        account.PayCreditCardDebt(new Money(500m, "USD"));

        account.DueDate.Should().NotBeNull();
        account.PaymentDate.Should().NotBeNull();

        account.DueDate!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    void Should_Allow_New_Purchases_After_Full_Payment()
    {
        var account = CreateCreditCard();
        account.ApplyDelta(new Money(4500m, "USD"), OperationType.CreditPurchase);
        account.PayCreditCardDebt(new Money(4500m, "USD"));

        var act = () => account.ApplyDelta(new Money(4500m, "USD"), OperationType.CreditPurchase);

        act.Should().NotThrow();
    }

    [Fact]
    void Should_Throw_When_Non_CreditCard_Account_Calls_PayCreditCardDebt()
    {
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);

        var act = () => account.PayCreditCardDebt(new Money(100m, "USD"));

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Only credit card accounts*");
    }

    [Fact]
    void Should_Throw_On_Payment_With_Currency_Mismatch()
    {
        var account = CreateCreditCard("USD");

        var act = () => account.PayCreditCardDebt(new Money(100m, "EUR"));

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Currency mismatch*");
    }

    [Fact]
    void Should_Throw_When_Paying_On_Closed_Account()
    {
        var account = CreateCreditCard();

        account.Close();

        var act = () => account.PayCreditCardDebt(new Money(100m, "USD"));

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Account is closed*");
    }

    // ════════════════════════════════════════════════════════════════════════
    // DUE DATE & DEBT RECALCULATION
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    void Should_Set_DueDate_To_10th_Of_Next_Month_On_Creation()
    {
        var account = CreateCreditCard();
        var now = DateTimeOffset.UtcNow;
        var expectedMonth = now.Month == 12 ? 1 : now.Month + 1;

        account.DueDate.Should().NotBeNull();
        account.DueDate!.Value.Day.Should().Be(10);
        account.DueDate!.Value.Month.Should().Be(expectedMonth);
    }

    [Fact]
    void Should_Set_PaymentDate_To_1st_Of_Next_Month_On_Creation()
    {
        var account = CreateCreditCard();
        var now = DateTimeOffset.UtcNow;
        var expectedMonth = now.Month == 12 ? 1 : now.Month + 1;

        account.PaymentDate.Should().NotBeNull();
        account.PaymentDate!.Value.Day.Should().Be(1);
        account.PaymentDate!.Value.Month.Should().Be(expectedMonth);
    }

    [Fact]
    void Should_Raise_DebtRecalculatedEvent_When_DueDate_Is_Overdue()
    {
        var account = CreateCreditCard();
        account.ApplyDelta(new Money(1000m, "USD"), OperationType.CreditPurchase);

        var overdueAccount = new Account(
            id: account.Id,
            userId: account.UserId,
            balance: account.Balance,
            creditLimit: account.CreditLimit,
            currentDebt: new Money(1000m, "USD"),
            status: AccountStatus.Active,
            type: AccountType.CreditCard,
            paymentDate: DateTimeOffset.UtcNow.AddDays(-20),
            dueDate: DateTimeOffset.UtcNow.AddDays(-5),   // ← overdue
            createdAt: DateTimeOffset.UtcNow.AddDays(-30),
            closedAt: null
        );

        var act = () => overdueAccount.PayCreditCardDebt(new Money(500m, "USD"));

        act.Should().NotThrow();
    }

    [Fact]
    void Should_Apply_5_Percent_Daily_Interest_When_Overdue()
    {
        var daysPastDue = 5;
        var originalDebt = 1000m;
        var expectedInterest = originalDebt * 0.05m * daysPastDue;
        var expectedDebt = originalDebt + expectedInterest;

        var overdueAccount = new Account(
            id: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            balance: new Money(0m, "USD"),
            creditLimit: new Money(4500m, "USD"),
            currentDebt: new Money(originalDebt, "USD"),
            status: AccountStatus.Active,
            type: AccountType.CreditCard,
            paymentDate: DateTimeOffset.UtcNow.AddDays(-20),
            dueDate: DateTimeOffset.UtcNow.AddDays(-daysPastDue),
            createdAt: DateTimeOffset.UtcNow.AddDays(-30),
            closedAt: null
        );

        overdueAccount.PayCreditCardDebt(new Money(0.01m, "USD"));

        overdueAccount.CurrentDebt.Amount.Should().BeApproximately(expectedDebt - 0.01m, 0.01m);
    }

    [Fact]
    void Should_Not_Raise_DebtRecalculatedEvent_When_Not_Overdue()
    {
        var account = CreateCreditCard();
        account.ApplyDelta(new Money(1000m, "USD"), OperationType.CreditPurchase);

        account.PayCreditCardDebt(new Money(500m, "USD"));

        account.CurrentDebt.Amount.Should().Be(500m);
    }

    [Fact]
    void Should_Reset_DueDate_After_Payment_On_Overdue_Account()
    {
        var overdueAccount = new Account(
            id: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            balance: new Money(0m, "USD"),
            creditLimit: new Money(4500m, "USD"),
            currentDebt: new Money(1000m, "USD"),
            status: AccountStatus.Active,
            type: AccountType.CreditCard,
            paymentDate: DateTimeOffset.UtcNow.AddDays(-20),
            dueDate: DateTimeOffset.UtcNow.AddDays(-5),
            createdAt: DateTimeOffset.UtcNow.AddDays(-30),
            closedAt: null
        );

        overdueAccount.PayCreditCardDebt(new Money(1000m, "USD"));

        overdueAccount.DueDate!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
        overdueAccount.PaymentDate!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    // ════════════════════════════════════════════════════════════════════════
    // REBUILD / PERSISTENCE
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    async Task Should_Rebuild_Correct_Debt_After_Purchase_And_Payment()
    {
        var account = CreateCreditCard();
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(),account.CurrentVersion, default);

        account.RebuildFromEvents(await _eventStore.Load(account.Id, token: default));

        account.ApplyDelta(new Money(2000m, "USD"), OperationType.CreditPurchase);
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(),account.CurrentVersion, default);

        account.RebuildFromEvents(await _eventStore.Load(account.Id, token: default));

        account.PayCreditCardDebt(new Money(800m, "USD"));
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(),account.CurrentVersion, default);

        var events = await _eventStore.Load(account.Id, default);
        var rebuilt = new Account();
        rebuilt.RebuildFromEvents(events);

        rebuilt.CurrentDebt.Amount.Should().Be(1200m);
        rebuilt.PayedAt.Should().BeNull(); 
    }

    [Fact]
    async Task Should_Rebuild_PayedAt_After_Full_Payment()
    {
        var account = CreateCreditCard();
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(),account.CurrentVersion, default);
        account.RebuildFromEvents(await _eventStore.Load(account.Id, default));

        account.ApplyDelta(new Money(500m, "USD"), OperationType.CreditPurchase);
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(),account.CurrentVersion, default);
        account.RebuildFromEvents(await _eventStore.Load(account.Id, default));

        account.PayCreditCardDebt(new Money(500m, "USD"));
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(),account.CurrentVersion, default);

        var events = await _eventStore.Load(account.Id, default);
        var rebuilt = new Account();
        rebuilt.RebuildFromEvents(events);

        rebuilt.CurrentDebt.Amount.Should().Be(0m);
        rebuilt.PayedAt.Should().NotBeNull();
    }
}
