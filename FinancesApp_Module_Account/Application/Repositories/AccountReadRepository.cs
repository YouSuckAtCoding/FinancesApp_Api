using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_Account.Domain;
using FinancesAppDatabase.Utils;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace FinancesApp_Module_Account.Application.Repositories;
public class AccountReadRepository : IAccountReadRepository
{
    private readonly ICommandFactory _commandFactory;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public AccountReadRepository(IDbConnectionFactory dbConnectionFactory,
                                 ICommandFactory commandFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _commandFactory = commandFactory;
    }

    public async Task<Account> GetAccountById(Guid id,
                                              SqlConnection? connection = null,
                                              CancellationToken token = default)
    {
        try
        {
            return await _commandFactory.ExecuteAsync(
            commandText: @"SELECT 
                                Id ,
                                UserId ,
                                Name ,
                                BalanceAmount ,
                                BalanceCurrency ,
                                CreditLimitAmount ,
                                CreditLimitCurrency ,
                                CurrentDebtAmount ,
                                CurrentDebtCurrency ,
                                PaymentDate ,
                                DueDate ,
                                Status ,
                                Type ,
                                CreatedAt ,
                                ClosedAt 
                         FROM [FinanceApp].[dbo].[Account] WHERE Id = @Id",
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new("@Id", SqlDbType.UniqueIdentifier) { Value = id }]
            },
            operation: async cmd =>
            {
                using var reader = await cmd.ExecuteReaderAsync(token);

                if (await reader.ReadAsync())
                {
                    return new Account(
                       id: reader.GetGuid("Id"),
                       userId: reader.GetNullableGuid("UserId"),
                       name: reader.GetString("Name"),
                       balance: new(reader.GetDecimal("BalanceAmount"), reader.GetString("BalanceCurrency")),
                       creditLimit: new(reader.GetDecimal("CreditLimitAmount"), reader.GetString("CreditLimitCurrency")),
                       currentDebt: new(reader.GetDecimal("CurrentDebtAmount"), reader.GetString("CurrentDebtCurrency")),
                       status: reader.GetEnum<AccountStatus>("Status"),
                       type: reader.GetEnum<AccountType>("Type"),
                       paymentDate: reader.GetNullableDateTimeOffset("PaymentDate"),
                       dueDate: reader.GetNullableDateTimeOffset("DueDate"),
                       createdAt: reader.GetDateTimeOffset("CreatedAt"),
                       closedAt: reader.GetNullableDateTimeOffset("ClosedAt"));
                }

                return new Account();
            },
            token);
        }
        catch
        {
            throw;
        }
    }
    public async Task<IReadOnlyList<Account>> GetAccounts(SqlConnection? connection = null,
                                                          CancellationToken token = default)
    {
        try
        {
            var result = new List<Account>();
            await _commandFactory.ExecuteAsync(
                commandText: @"SELECT 
                                    Id ,
                                    UserId ,
                                    Name ,
                                    BalanceAmount ,
                                    BalanceCurrency ,
                                    CreditLimitAmount ,
                                    CreditLimitCurrency ,
                                    CurrentDebtAmount ,
                                    CurrentDebtCurrency ,
                                    PaymentDate ,
                                    DueDate ,
                                    Status ,
                                    Type ,
                                    CreatedAt ,
                                    ClosedAt 
                             FROM [FinanceApp].[dbo].[Account]",
                connection: connection,
                options: new CreateSqlCommandOptions(),
                operation: async cmd =>
                {
                    using var reader = await cmd.ExecuteReaderAsync(token);

                    while (await reader.ReadAsync())
                    {
                        result.Add(new Account(
                            id: reader.GetGuid("Id"),
                            userId: reader.GetNullableGuid("UserId"),
                            name: reader.GetString("Name"),
                            balance: new(reader.GetDecimal("BalanceAmount"), reader.GetString("BalanceCurrency")),
                            creditLimit: new(reader.GetDecimal("CreditLimitAmount"), reader.GetString("CreditLimitCurrency")),
                            currentDebt: new(reader.GetDecimal("CurrentDebtAmount"), reader.GetString("CurrentDebtCurrency")),
                            status: reader.GetEnum<AccountStatus>("Status"),
                            type: reader.GetEnum<AccountType>("Type"),
                            paymentDate: reader.GetNullableDateTimeOffset("PaymentDate"),
                            dueDate: reader.GetNullableDateTimeOffset("DueDate"),
                            createdAt: reader.GetDateTimeOffset("CreatedAt"),
                            closedAt: reader.GetNullableDateTimeOffset("ClosedAt"))
                        );
                    }
                },
                token);

            return result;
        }
        catch
        {
            throw;
        }
    }
    public async Task<IReadOnlyList<Account>> GetActiveAccounts(SqlConnection? connection = null,
                                                                CancellationToken token = default)
    {
        try
        {
            var result = new List<Account>();

            await _commandFactory.ExecuteAsync(
            commandText: @"SELECT 
                                Id ,
                                UserId ,
                                Name ,
                                BalanceAmount ,
                                BalanceCurrency ,
                                CreditLimitAmount ,
                                CreditLimitCurrency ,
                                CurrentDebtAmount ,
                                CurrentDebtCurrency ,
                                PaymentDate ,
                                DueDate ,
                                Status ,
                                Type ,
                                CreatedAt ,
                                ClosedAt 
                         FROM [FinanceApp].[dbo].[Account] WHERE Status = @Status",
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new("@Status", SqlDbType.Int) { Value = AccountStatus.Active }]
            },
            operation: async cmd =>
            {
                using var reader = await cmd.ExecuteReaderAsync(token);

                while (await reader.ReadAsync())
                {
                    result.Add(new Account(
                        id: reader.GetGuid("Id"),
                        userId: reader.GetNullableGuid("UserId"),
                        name: reader.GetString("Name"),
                        balance: new(reader.GetDecimal("BalanceAmount"), reader.GetString("BalanceCurrency")),
                        creditLimit: new(reader.GetDecimal("CreditLimitAmount"), reader.GetString("CreditLimitCurrency")),
                        currentDebt: new(reader.GetDecimal("CurrentDebtAmount"), reader.GetString("CurrentDebtCurrency")),
                        status: reader.GetEnum<AccountStatus>("Status"),
                        type: reader.GetEnum<AccountType>("Type"),
                        paymentDate: reader.GetNullableDateTimeOffset("PaymentDate"),
                        dueDate: reader.GetNullableDateTimeOffset("DueDate"),
                        createdAt: reader.GetDateTimeOffset("CreatedAt"),
                        closedAt: reader.GetNullableDateTimeOffset("ClosedAt"))
                    );
                }
            }, token);

            return result;
        }
        catch
        {
            throw;
        }
    }
    public async Task<IReadOnlyList<Account>> GetAccountByType(AccountType type,
                                                               SqlConnection? connection = null,
                                                               CancellationToken token = default)
    {
        var result = new List<Account>();
        try
        {
            await _commandFactory.ExecuteAsync(
            commandText: @"SELECT 
                                Id ,
                                UserId ,
                                Name ,
                                BalanceAmount ,
                                BalanceCurrency ,
                                CreditLimitAmount ,
                                CreditLimitCurrency ,
                                CurrentDebtAmount ,
                                CurrentDebtCurrency ,
                                PaymentDate ,
                                DueDate ,
                                Status ,
                                Type ,
                                CreatedAt ,
                                ClosedAt 
                         FROM [FinanceApp].[dbo].[Account] WHERE Type = @Type",
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new("@Type", SqlDbType.Int) { Value = type }]
            },
            operation: async cmd =>
            {
                using var reader = await cmd.ExecuteReaderAsync(token);

                while (await reader.ReadAsync())
                {
                    result.Add(new Account(
                        id: reader.GetGuid("Id"),
                        userId: reader.GetNullableGuid("UserId"),
                        name: reader.GetString("Name"),
                        balance: new(reader.GetDecimal("BalanceAmount"), reader.GetString("BalanceCurrency")),
                        creditLimit: new(reader.GetDecimal("CreditLimitAmount"), reader.GetString("CreditLimitCurrency")),
                        currentDebt: new(reader.GetDecimal("CurrentDebtAmount"), reader.GetString("CurrentDebtCurrency")),
                        status: reader.GetEnum<AccountStatus>("Status"),
                        type: reader.GetEnum<AccountType>("Type"),
                        paymentDate: reader.GetNullableDateTimeOffset("PaymentDate"),
                        dueDate: reader.GetNullableDateTimeOffset("DueDate"),
                        createdAt: reader.GetDateTimeOffset("CreatedAt"),
                        closedAt: reader.GetNullableDateTimeOffset("ClosedAt"))
                    );
                }
            }, token);

            return result;
        }
        catch
        {
            throw;
        }
    }
}
