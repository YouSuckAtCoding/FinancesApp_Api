using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_Account.Domain;
using Microsoft.Data.SqlClient;

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

    public async Task<bool> CreateAccountAsync(Account account, CancellationToken token = default)
    {
        const string InsertCommandText = @"INSERT INTO [FinanceApp].[dbo].[Account] 
                         (Id, UserId, Name, BalanceAmount, BalanceCurrency, CreditLimitAmount, CreditLimitCurrency,
                             CurrentDebtAmount, CurrentDebtCurrency, Type, Status, PaymentDate, DueDate, CreatedAt, ClosedAt)
                         VALUES 
                            (@Id, @UserId, @Name, @BalanceAmount, @BalanceCurrency, @CreditLimitAmount, @CreditLimitCurrency,
                             @CurrentDebtAmount, @CurrentDebtCurrency, @Type, @Status, @PaymentDate, @DueDate, @CreatedAt, @ClosedAt)";

        var parameters = new Dictionary<string, object>
        {
            { "@Id", account.Id },
            { "@UserId", (object?)account.UserId ?? DBNull.Value },
            { "@Name", account.Name },
            { "@BalanceAmount", account.Balance.Amount },
            { "@BalanceCurrency", account.Balance.Currency },
            { "@CreditLimitAmount", account.CreditLimit.Amount },
            { "@CreditLimitCurrency", account.CreditLimit.Currency },
            { "@CurrentDebtAmount", account.CurrentDebt.Amount },
            { "@CurrentDebtCurrency", account.CurrentDebt.Currency },
            { "@Type", account.Type },
            { "@Status", account.Status },
            { "@PaymentDate", (object?)account.PaymentDate ?? DBNull.Value },
            { "@DueDate", (object?)account.DueDate ?? DBNull.Value },
            { "@CreatedAt", account.CreatedAt },
            { "@ClosedAt", (object?)account.ClosedAt ?? DBNull.Value }
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

    public async Task<bool> UpdateAccountAsync(Account account, CancellationToken token = default)
    {
        const string UpdateCommandText = @"UPDATE [FinanceApp].[dbo].[Account]
                         SET Name = @Name,
                             BalanceAmount = @BalanceAmount,
                             BalanceCurrency = @BalanceCurrency,
                             CreditLimitAmount = @CreditLimitAmount,
                             CreditLimitCurrency = @CreditLimitCurrency,
                             CurrentDebtAmount = @CurrentDebtAmount,
                             CurrentDebtCurrency = @CurrentDebtCurrency,
                             Status = @Status,
                             PaymentDate = @PaymentDate,
                             DueDate = @DueDate,
                             ClosedAt = @ClosedAt
                         WHERE Id = @Id";

        var parameters = new Dictionary<string, object>
        {
            { "@Id", account.Id },
            { "@Name", account.Name },
            { "@BalanceAmount", account.Balance.Amount },
            { "@BalanceCurrency", account.Balance.Currency },
            { "@CreditLimitAmount", account.CreditLimit.Amount },
            { "@CreditLimitCurrency", account.CreditLimit.Currency },
            { "@CurrentDebtAmount", account.CurrentDebt.Amount },
            { "@CurrentDebtCurrency", account.CurrentDebt.Currency },
            { "@Status", account.Status },
            { "@PaymentDate", (object?)account.PaymentDate ?? DBNull.Value },
            { "@DueDate", (object?)account.DueDate ?? DBNull.Value },
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

    public async Task<bool> DeleteAccountAsync(Guid accountId, CancellationToken token = default)
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
}