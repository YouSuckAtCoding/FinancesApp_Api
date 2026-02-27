using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Tests.AccountTests.Repositories;

public class AccountRepositoryTests : IClassFixture<SqlFixture>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICommandFactory _commandFactory;
    private readonly IAccountRepository _accountRepository;
    
    private const string TableName = "[FinanceApp].[dbo].[Account]";

    public AccountRepositoryTests(SqlFixture fixture)
    {
        _connectionFactory = fixture.ConnectionFactory;
        _commandFactory = fixture.CommandFactory;
        _accountRepository = new AccountRepository(_connectionFactory, _commandFactory);     
    }

    [Fact]
    public async Task CreateAccountAsync_Should_Insert_Account_To_Database()
    {
        // Arrange
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccountTable(connection);

            var account = new Account(
                userId: Guid.NewGuid(),
                name: "Test Checking Account",
                balance: new Money(1500.00m, "USD"),
                type: AccountType.Checking
            );

            // Act
            var result = await _accountRepository.CreateAccountAsync(account);

            // Assert
            result.Should().BeTrue();

            var retrieved = await GetAccountByIdAsync(account.Id, connection);
            retrieved.Should().NotBeNull();
            retrieved.Id.Should().Be(account.Id);
            retrieved.Name.Should().Be("Test Checking Account");
            retrieved.Balance.Amount.Should().Be(1500.00m);
            retrieved.Balance.Currency.Should().Be("USD");
            retrieved.Type.Should().Be(AccountType.Checking);
            retrieved.Status.Should().Be(AccountStatus.Active);
        });
    }

    [Fact]
    public async Task CreateAccountAsync_Should_Insert_CreditCard_Account_With_CreditLimit()
    {
        // Arrange
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccountTable(connection);

            var account = new Account(
                userId: Guid.NewGuid(),
                name: "Test Credit Card",
                balance: new Money(500.00m, "USD"),
                type: AccountType.CreditCard
            );

            // Act
            var result = await _accountRepository.CreateAccountAsync(account);

            // Assert
            result.Should().BeTrue();

            var retrieved = await GetAccountByIdAsync(account.Id, connection);
            retrieved.Should().NotBeNull();
            retrieved.Id.Should().Be(account.Id);
            retrieved.Name.Should().Be("Test Credit Card");
            retrieved.Type.Should().Be(AccountType.CreditCard);
            retrieved.CreditLimit.Amount.Should().Be(4500);
            retrieved.CurrentDebt.Amount.Should().Be(0m);
        });
    }

    [Fact]
    public async Task CreateAccountAsync_Should_Insert_Account_Without_UserId()
    {
        // Arrange
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccountTable(connection);

            var account = new Account(
                userId: null,
                name: "Test Cash Account",
                balance: new Money(200.00m, "BRL"),
                type: AccountType.Cash
            );

            // Act
            var result = await _accountRepository.CreateAccountAsync(account);

            // Assert
            result.Should().BeTrue();

            var retrieved = await GetAccountByIdAsync(account.Id, connection);
            retrieved.Should().NotBeNull();
            retrieved.UserId.Should().BeNull();
            retrieved.Balance.Currency.Should().Be("BRL");
        });
    }

    [Fact]
    public async Task UpdateAccountAsync_Should_Update_Account_Name()
    {
        // Arrange
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccountTable(connection);

            var account = new Account(
                userId: Guid.NewGuid(),
                name: "Original Name",
                balance: new Money(1000.00m, "USD"),
                type: AccountType.Checking
            );

            await _accountRepository.CreateAccountAsync(account);

            // Act
            account.UpdateName("Updated Name");
            var result = await _accountRepository.UpdateAccountAsync(account);

            // Assert
            result.Should().BeTrue();

            var retrieved = await GetAccountByIdAsync(account.Id, connection);
            retrieved.Name.Should().Be("Updated Name");
        });
    }

    [Fact]
    public async Task UpdateAccountAsync_Should_Update_Account_Balance()
    {
        // Arrange
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccountTable(connection);

            var account = new Account(
                userId: Guid.NewGuid(),
                name: "Test Account",
                balance: new Money(1000.00m, "USD"),
                type: AccountType.Checking
            );

            await _accountRepository.CreateAccountAsync(account);

            // Act
            account.ApplyDelta(new Money(500.00m, "USD"));
            var result = await _accountRepository.UpdateAccountAsync(account);

            // Assert
            result.Should().BeTrue();

            var retrieved = await GetAccountByIdAsync(account.Id, connection);
            retrieved.Balance.Amount.Should().Be(1500.00m);
        });
    }

    [Fact]
    public async Task UpdateAccountAsync_Should_Update_Account_Status_To_Closed()
    {
        // Arrange
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccountTable(connection);

            var account = new Account(
                id: Guid.NewGuid(),
                userId: Guid.NewGuid(),
                name: "Test Account",
                balance: new Money(100.00m, "USD"),
                type: AccountType.Cash
            );

            await _accountRepository.CreateAccountAsync(account);

            // Act
            account.ApplyDelta(new Money(-100.00m, "USD")); // Zero balance
            account.Close();
            var result = await _accountRepository.UpdateAccountAsync(account);

            // Assert
            result.Should().BeTrue();

            var retrieved = await GetAccountByIdAsync(account.Id, connection);
            retrieved.Status.Should().Be(AccountStatus.Closed);
            retrieved.ClosedAt.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task UpdateAccountAsync_Should_Update_CreditCard_Debt()
    {
        // Arrange
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccountTable(connection);

            var account = new Account(
                id: Guid.NewGuid(),
                userId: Guid.NewGuid(),
                name: "Test Credit Card",
                balance: new Money(500.00m, "USD"),
                type: AccountType.CreditCard
            );

            await _accountRepository.CreateAccountAsync(account);

            // Act
            account.ApplyDelta(new Money(200.00m, "USD"), OperationType.CreditPurchase); // Add debt
            var result = await _accountRepository.UpdateAccountAsync(account);

            // Assert
            result.Should().BeTrue();

            var retrieved = await GetAccountByIdAsync(account.Id, connection);
            retrieved.CurrentDebt.Amount.Should().Be(account.CurrentDebt.Amount); // CreditLimit - debt
        });
    }

    [Fact]
    public async Task UpdateAccountAsync_Should_Return_False_When_Account_Does_Not_Exist()
    {
        // Arrange
        var nonExistentAccount = new Account(
            id: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            name: "Non-existent Account",
            balance: new Money(1000.00m, "USD"),
            creditLimit: new Money(5000.00m, "USD"),
            currentDebt: new Money(0m, "USD"),
            status: AccountStatus.Active,
            type: AccountType.Checking,
            paymentDate: null,
            dueDate: null,
            createdAt: DateTimeOffset.UtcNow,
            closedAt: null
        );

        // Act
        var result = await _accountRepository.UpdateAccountAsync(nonExistentAccount);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAccountAsync_Should_Remove_Account_From_Database()
    {
        // Arrange
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccountTable(connection);

            var account = new Account(
                userId: Guid.NewGuid(),
                name: "Account To Delete",
                balance: new Money(500.00m, "USD"),
                type: AccountType.Cash
            );

            await _accountRepository.CreateAccountAsync(account);

            // Act
            var result = await _accountRepository.DeleteAccountAsync(account.Id);

            // Assert
            result.Should().BeTrue();

            var retrieved = await GetAccountByIdAsync(account.Id, connection);
            retrieved.Id.Should().Be(Guid.Empty);
        });
    }

    [Fact]
    public async Task DeleteAccountAsync_Should_Return_False_When_Account_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _accountRepository.DeleteAccountAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAccountAsync_Should_Handle_Multiple_Accounts()
    {
        // Arrange
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccountTable(connection);

            var accounts = new List<Account>
            {
                new Account(Guid.NewGuid(), new Guid(), "Account 1", new Money(100.00m, "USD"), AccountType.Cash),
                new Account(Guid.NewGuid(), new Guid(), "Account 2", new Money(200.00m, "USD"), AccountType.Checking),
                new Account(Guid.NewGuid(), new Guid(), "Account 3", new Money(300.00m, "USD"), AccountType.CreditCard)
            };

            // Act
            foreach (var account in accounts)
            {
                var result = await _accountRepository.CreateAccountAsync(account);
                result.Should().BeTrue();
            }

            // Assert
            var allAccounts = await GetAllAccountsAsync(connection);
            allAccounts.Count.Should().Be(3);
        });
    }

    [Fact]
    public async Task UpdateAccountAsync_Should_Preserve_Immutable_Fields()
    {
        // Arrange
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccountTable(connection);

            var userId = Guid.NewGuid();
            var account = new Account(
                userId: userId,
                name: "Test Account",
                balance: new Money(1000.00m, "USD"),
                type: AccountType.Checking
            );

            await _accountRepository.CreateAccountAsync(account);
            var originalCreatedAt = account.CreatedAt;

            // Act
            account.UpdateName("Updated Name");
            await _accountRepository.UpdateAccountAsync(account);

            // Assert
            var retrieved = await GetAccountByIdAsync(account.Id, connection);
            retrieved.UserId.Should().Be(userId);
            retrieved.Type.Should().Be(AccountType.Checking);
            retrieved.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromSeconds(1));
        });
    }

    #region Private Helper Methods

    private async Task<Account> GetAccountByIdAsync(Guid accountId, SqlConnection connection)
    {
        const string selectCommandText = @"SELECT Id, UserId, Name, BalanceAmount, BalanceCurrency, 
                                          CreditLimitAmount, CreditLimitCurrency, CurrentDebtAmount, CurrentDebtCurrency,
                                          PaymentDate, DueDate, Status, Type, CreatedAt, ClosedAt
                                          FROM [FinanceApp].[dbo].[Account]
                                          WHERE Id = @Id";

        return await _commandFactory.ExecuteAsync(
            commandText: selectCommandText,
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new SqlParameter("@Id", accountId)]
            },
            operation: async command =>
            {
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new Account(
                        id: reader.GetGuid(reader.GetOrdinal("Id")),
                        userId: reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetGuid(reader.GetOrdinal("UserId")),
                        name: reader.GetString(reader.GetOrdinal("Name")),
                        balance: new Money(
                            reader.GetDecimal(reader.GetOrdinal("BalanceAmount")),
                            reader.GetString(reader.GetOrdinal("BalanceCurrency"))),
                        creditLimit: new Money(
                            reader.GetDecimal(reader.GetOrdinal("CreditLimitAmount")),
                            reader.GetString(reader.GetOrdinal("CreditLimitCurrency"))),
                        currentDebt: new Money(
                            reader.GetDecimal(reader.GetOrdinal("CurrentDebtAmount")),
                            reader.GetString(reader.GetOrdinal("CurrentDebtCurrency"))),
                        status: (AccountStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                        type: (AccountType)reader.GetInt32(reader.GetOrdinal("Type")),
                        paymentDate: reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ? null : reader.GetDateTimeOffset(reader.GetOrdinal("PaymentDate")),
                        dueDate: reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : reader.GetDateTimeOffset(reader.GetOrdinal("DueDate")),
                        createdAt: reader.GetDateTimeOffset(reader.GetOrdinal("CreatedAt")),
                        closedAt: reader.IsDBNull(reader.GetOrdinal("ClosedAt")) ? null : reader.GetDateTimeOffset(reader.GetOrdinal("ClosedAt"))
                    );
                }

                return new Account(); // Return empty account if not found
            },
            default);
    }

    private async Task<List<Account>> GetAllAccountsAsync(SqlConnection connection)
    {
        const string selectCommandText = @"SELECT Id, UserId, Name, BalanceAmount, BalanceCurrency, 
                                          CreditLimitAmount, CreditLimitCurrency, CurrentDebtAmount, CurrentDebtCurrency,
                                          PaymentDate, DueDate, Status, Type, CreatedAt, ClosedAt
                                          FROM [FinanceApp].[dbo].[Account]";

        return await _commandFactory.ExecuteAsync(
            commandText: selectCommandText,
            connection: connection,
            options: new CreateSqlCommandOptions(),
            operation: async command =>
            {
                var accounts = new List<Account>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    accounts.Add(new Account(
                        id: reader.GetGuid(reader.GetOrdinal("Id")),
                        userId: reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetGuid(reader.GetOrdinal("UserId")),
                        name: reader.GetString(reader.GetOrdinal("Name")),
                        balance: new Money(
                            reader.GetDecimal(reader.GetOrdinal("BalanceAmount")),
                            reader.GetString(reader.GetOrdinal("BalanceCurrency"))),
                        creditLimit: new Money(
                            reader.GetDecimal(reader.GetOrdinal("CreditLimitAmount")),
                            reader.GetString(reader.GetOrdinal("CreditLimitCurrency"))),
                        currentDebt: new Money(
                            reader.GetDecimal(reader.GetOrdinal("CurrentDebtAmount")),
                            reader.GetString(reader.GetOrdinal("CurrentDebtCurrency"))),
                        status: (AccountStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                        type: (AccountType)reader.GetInt32(reader.GetOrdinal("Type")),
                        paymentDate: reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ? null : reader.GetDateTimeOffset(reader.GetOrdinal("PaymentDate")),
                        dueDate: reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : reader.GetDateTimeOffset(reader.GetOrdinal("DueDate")),
                        createdAt: reader.GetDateTimeOffset(reader.GetOrdinal("CreatedAt")),
                        closedAt: reader.IsDBNull(reader.GetOrdinal("ClosedAt")) ? null : reader.GetDateTimeOffset(reader.GetOrdinal("ClosedAt"))
                    ));
                }

                return accounts;
            },
            default);
    }

    private async Task ClearAccountTable(SqlConnection connection)
    {
        await _commandFactory.ExecuteAsync(
            commandText: $"DELETE FROM {TableName}",
            connection: connection,
            options: new CreateSqlCommandOptions(),
            operation: async command =>
            {
                await command.ExecuteNonQueryAsync();
            },
            default);
    }

    #endregion
}
