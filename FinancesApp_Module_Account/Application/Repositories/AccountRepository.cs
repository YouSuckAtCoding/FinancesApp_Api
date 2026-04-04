using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Module_Account.Application.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ICommandFactory _commandFactory;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public AccountRepository(IDbConnectionFactory dbConnectionFactory,
                                 ICommandFactory commandFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _commandFactory = commandFactory;
    }

    public async Task<bool> CreateAccountAsync(Account account,
                                               SqlConnection? connection = null,
                                               CancellationToken token = default)
    {
        const string InsertCommandText = @"INSERT INTO [FinanceApp].[dbo].[Account] 
                         (Id, UserId, BalanceAmount, BalanceCurrency, CreditLimitAmount, CreditLimitCurrency,
                             CurrentDebtAmount, CurrentDebtCurrency, Type, Status, PaymentDate, DueDate, ClosedAt)
                         VALUES 
                            (@Id, @UserId, @BalanceAmount, @BalanceCurrency, @CreditLimitAmount, @CreditLimitCurrency,
                             @CurrentDebtAmount, @CurrentDebtCurrency, @Type, @Status, @PaymentDate, @DueDate, @ClosedAt)";
        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "@Id", account.Id },
                { "@UserId", account.UserId},
                { "@BalanceAmount", account.Balance.Amount },
                { "@BalanceCurrency", account.Balance.Currency },
                { "@CreditLimitAmount", account.CreditLimit.Amount },
                { "@CreditLimitCurrency", account.CreditLimit.Currency },
                { "@CurrentDebtAmount", account.CurrentDebt.Amount },
                { "@CurrentDebtCurrency", account.CurrentDebt.Currency },
                { "@Type", account.Type },
                { "@Status", account.Status },
                { "@PaymentDate", (object?)account.PaymentDate ?? DBNull.Value },
                { "@DueDate", DBNull.Value },
                { "@ClosedAt", DBNull.Value }
            };

            var rowsAffected = await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                options: new CreateSqlCommandOptions
                {
                    Parameters = [.. parameters.Select(p => new SqlParameter(p.Key, p.Value))]
                },
                operation: async command => await command.ExecuteNonQueryAsync(token),
                token);

            return rowsAffected > 0;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> UpdateAccountAsync(Account account,
                                               SqlConnection? connection = null,
                                               CancellationToken token = default)
    {
        const string UpdateCommandText = @"UPDATE [FinanceApp].[dbo].[Account]
                         SET BalanceAmount = @BalanceAmount,
                             BalanceCurrency = @BalanceCurrency,
                             CreditLimitAmount = @CreditLimitAmount,
                             CreditLimitCurrency = @CreditLimitCurrency,
                             CurrentDebtAmount = @CurrentDebtAmount,
                             CurrentDebtCurrency = @CurrentDebtCurrency,
                             Status = @Status,
                             PaymentDate = @PaymentDate,
                             DueDate = @DueDate,
                             UpdatedAt = @UpdatedAt,
                             ClosedAt = @ClosedAt
                         WHERE Id = @Id";

        var parameters = new Dictionary<string, object>
        {
            { "@Id", account.Id },
            { "@BalanceAmount", account.Balance.Amount },
            { "@BalanceCurrency", account.Balance.Currency },
            { "@CreditLimitAmount", account.CreditLimit.Amount },
            { "@CreditLimitCurrency", account.CreditLimit.Currency },
            { "@CurrentDebtAmount", account.CurrentDebt.Amount },
            { "@CurrentDebtCurrency", account.CurrentDebt.Currency },
            { "@Status", account.Status },
            { "@PaymentDate", (object?)account.PaymentDate ?? DBNull.Value },
            { "@DueDate", (object?)account.DueDate ?? DBNull.Value },
            { "@UpdatedAt", DateTimeOffset.UtcNow },
            { "@ClosedAt", (object?)account.ClosedAt ?? DBNull.Value }
        };

        var rowsAffected = await _commandFactory.ExecuteAsync(
            commandText: UpdateCommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [.. parameters.Select(p => new SqlParameter(p.Key, p.Value))]
            },
            operation: async command => await command.ExecuteNonQueryAsync(token),
            token);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAccountAsync(Guid accountId,
                                               SqlConnection? connection = null,
                                               CancellationToken token = default)
    {
        const string DeleteCommandText = @"DELETE FROM [FinanceApp].[dbo].[Account]
                                          WHERE Id = @Id";

        var parameters = new Dictionary<string, object>
        {
            { "@Id", accountId }
        };

        var rowsAffected = await _commandFactory.ExecuteAsync(
            commandText: DeleteCommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [.. parameters.Select(p => new SqlParameter(p.Key, p.Value))]
            },
            operation: async command => await command.ExecuteNonQueryAsync(token),
            token);

        return rowsAffected > 0;
    }

    public async Task ApplyDepositAsync(Guid accountId, Money amount, CancellationToken token = default)
    {
        const string CommandText = """
            UPDATE [FinanceApp].[dbo].[Account]
            SET BalanceAmount = BalanceAmount + @amount,
                UpdatedAt      = SYSDATETIMEOFFSET()
            WHERE Id = @id
            """;

        await _commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@amount", amount.Amount),
                    new SqlParameter("@id",     accountId)
                ]
            },
            operation: async cmd => await cmd.ExecuteNonQueryAsync(token),
            token);
    }

    public async Task ApplyWithdrawAsync(Guid accountId, Money amount, CancellationToken token = default)
    {
        const string CommandText = """
            UPDATE [FinanceApp].[dbo].[Account]
            SET BalanceAmount = BalanceAmount - @amount,
                UpdatedAt      = SYSDATETIMEOFFSET()
            WHERE Id = @id
            """;

        await _commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@amount", amount.Amount),
                    new SqlParameter("@id",     accountId)
                ]
            },
            operation: async cmd => await cmd.ExecuteNonQueryAsync(token),
            token);
    }

    public async Task UpdateDebtAsync(Guid accountId, Money newDebt, CancellationToken token = default)
    {
        const string CommandText = """
            UPDATE [FinanceApp].[dbo].[Account]
            SET CurrentDebtAmount   = @amount,
                CurrentDebtCurrency = @currency,
                UpdatedAt            = SYSDATETIMEOFFSET()
            WHERE Id = @id
            """;

        await _commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@amount",   newDebt.Amount),
                    new SqlParameter("@currency", newDebt.Currency),
                    new SqlParameter("@id",       accountId)
                ]
            },
            operation: async cmd => await cmd.ExecuteNonQueryAsync(token),
            token);
    }

    public async Task UpdateCreditLimitAsync(Guid accountId, Money creditLimit, CancellationToken token = default)
    {
        const string CommandText = """
            UPDATE [FinanceApp].[dbo].[Account]
            SET CreditLimitAmount   = @amount,
                CreditLimitCurrency = @currency,
                UpdatedAt            = SYSDATETIMEOFFSET()
            WHERE Id = @id
            """;

        await _commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@amount",   creditLimit.Amount),
                    new SqlParameter("@currency", creditLimit.Currency),
                    new SqlParameter("@id",       accountId)
                ]
            },
            operation: async cmd => await cmd.ExecuteNonQueryAsync(token),
            token);
    }

    public async Task ApplyPaymentAsync(Guid accountId, Money paymentAmount, CancellationToken token = default)
    {
        const string CommandText = """
            UPDATE [FinanceApp].[dbo].[Account]
            SET CurrentDebtAmount = CASE
                                        WHEN CurrentDebtAmount - @amount <= 0 THEN 0
                                        ELSE CurrentDebtAmount - @amount
                                    END,
                UpdatedAt          = SYSDATETIMEOFFSET()
            WHERE Id = @id
            """;

        await _commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@amount", paymentAmount.Amount),
                    new SqlParameter("@id",     accountId)
                ]
            },
            operation: async cmd => await cmd.ExecuteNonQueryAsync(token),
            token);
    }

    public async Task UpdateDueDateAsync(Guid accountId, DateTimeOffset dueDate, CancellationToken token = default)
    {
        const string CommandText = """
            UPDATE [FinanceApp].[dbo].[Account]
            SET DueDate   = @dueDate,
                UpdatedAt = SYSDATETIMEOFFSET()
            WHERE Id = @id
            """;

        await _commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@dueDate", dueDate),
                    new SqlParameter("@id",      accountId)
                ]
            },
            operation: async cmd => await cmd.ExecuteNonQueryAsync(token),
            token);
    }

    public async Task CloseAccountAsync(Guid accountId, DateTimeOffset closedAt, CancellationToken token = default)
    {
        const string CommandText = """
            UPDATE [FinanceApp].[dbo].[Account]
            SET Status    = @status,
                ClosedAt  = @closedAt,
                UpdatedAt = SYSDATETIMEOFFSET()
            WHERE Id = @id
            """;

        await _commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@status",   (int)AccountStatus.Closed),
                    new SqlParameter("@closedAt", closedAt),
                    new SqlParameter("@id",       accountId)
                ]
            },
            operation: async cmd => await cmd.ExecuteNonQueryAsync(token),
            token);
    }

    public async Task SyncStateAsync(Guid accountId, Money balance, Money debt, AccountType type, CancellationToken token = default)
    {
        const string CommandText = """
            UPDATE [FinanceApp].[dbo].[Account]
            SET BalanceAmount        = @balanceAmount,
                BalanceCurrency      = @balanceCurrency,
                CurrentDebtAmount    = @debtAmount,
                CurrentDebtCurrency  = @debtCurrency,
                Type                 = @type,
                UpdatedAt            = SYSDATETIMEOFFSET()
            WHERE Id = @id
            """;

        await _commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@balanceAmount",   balance.Amount),
                    new SqlParameter("@balanceCurrency", balance.Currency),
                    new SqlParameter("@debtAmount",      debt.Amount),
                    new SqlParameter("@debtCurrency",    debt.Currency),
                    new SqlParameter("@type",            (int)type),
                    new SqlParameter("@id",              accountId)
                ]
            },
            operation: async cmd => await cmd.ExecuteNonQueryAsync(token),
            token);
    }
}