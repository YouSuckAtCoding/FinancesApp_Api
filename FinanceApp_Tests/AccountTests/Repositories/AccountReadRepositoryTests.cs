using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Tests.AccountTests.Repositories;
public class AccountReadRepositoryTests : IClassFixture<SqlFixture>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICommandFactory _commandFactory;
    private readonly IAccountReadRepository _accountReadRepository;
    private readonly IUserRepository _userRepository;
    private readonly SqlFixture _fixture;

    private const string TableName = "[FinanceApp].[dbo].[Account]";
    private const string InsertCommandText = 
        $" INSERT INTO {TableName} (Name, UserId, BalanceAmount, CreatedAt, Type, Status)" +
        " OUTPUT INSERTED.Id " +
        " VALUES (@Name, @UserId, @BalanceAmount, @CreatedAt, @Type, @Status)";

    public AccountReadRepositoryTests(SqlFixture fixture)
    {
        _connectionFactory = fixture.ConnectionFactory;
        _commandFactory = fixture.CommandFactory;
        _accountReadRepository = new AccountReadRepository(_connectionFactory, _commandFactory);
        _userRepository = new UserRepository(_connectionFactory, _commandFactory);
        _fixture = fixture; 
    }

    [Fact]
    public async Task Should_Reach_TestDatabase_And_Try_To_Retrieve_An_Account()
    {
       var result = await _commandFactory.ExecuteAsync(
           commandText: $"Select TOP 1 * from {TableName}",
           options: new CreateSqlCommandOptions(),
           operation: async command => {
               return await command.ExecuteReaderAsync();
           },
           default);

       result.Should().NotBeNull();
        
    }
    [Fact]
    public async Task Should_Return_Account_By_Id()
    {
        Dictionary<string, object> parameters = GetBaseAccountInsertParameters();

        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            Guid newUserId = await CreateNewUser(connection);

            Guid insertedId = await _commandFactory.ExecuteAsync(
            commandText: InsertCommandText,
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters = [ new("@Name", parameters["@Name"]),
                               new("@UserId", newUserId),
                               new("@BalanceAmount", parameters["@BalanceAmount"]),
                               new("@CreatedAt", parameters["@CreatedAt"]),
                               new("@Type", parameters["@Type"]),
                               new("@Status", parameters["@Status"])
                ]
            },
            operation: async command =>
            {

                var accountIds = new List<Guid>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    accountIds.Add(reader.GetGuid(0));

                return accountIds.First();
            },
            default);

           var retrieved = await _accountReadRepository.GetAccountById(insertedId, 
                                                                        connection, 
                                                                        default);
            retrieved.Id.Should().Be(insertedId);   
        });
    }
    [Fact]
    public async Task Should_Return_All_Accounts()
    {
        Dictionary<string, object> parameters = GetBaseAccountInsertParameters();

        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccounTable(connection);


            for (int i = 0; i < 5; i++)
            {
                Guid newUserId = await CreateNewUser(connection);

                await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                  Parameters = [ new("@Name", parameters["@Name"]),
                                 new("@UserId", newUserId),
                                 new("@BalanceAmount", parameters["@BalanceAmount"]),
                                 new("@CreatedAt", parameters["@CreatedAt"]),
                                 new("@Type", parameters["@Type"]),
                                 new("@Status", parameters["@Status"])
                  ]
                },
                operation: async command =>
                {
                    await command.ExecuteNonQueryAsync();
                },
                default);
            }
            var retrieved = await _accountReadRepository.GetAccounts(connection,
                                                                     default);
            retrieved.Count.Should().Be(5);
        });
    }
    [Fact]
    public async Task Should_Return_All_Active_Accounts()
    {
          
        int activeAccountsExpected = 3;

        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccounTable(connection);

            for (int i = 0; i < 5; i++)
            {
                Guid newUserId = await CreateNewUser(connection);

                Dictionary<string, object> parameters = GetBaseAccountInsertParameters(i);

                await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters = [new("@Name", parameters["@Name"]),
                                 new("@UserId", newUserId),
                                 new("@BalanceAmount", parameters["@BalanceAmount"]),
                                 new("@CreatedAt", parameters["@CreatedAt"]),
                                 new("@Type", parameters["@Type"]),
                                 new("@Status", parameters["@Status"])
                   ]
                },
                operation: async command =>
                {
                    await command.ExecuteNonQueryAsync();
                },
                default);
            }
            var retrieved = await _accountReadRepository.GetActiveAccounts(connection, default);

            retrieved.Count.Should().Be(activeAccountsExpected);
        });
    }

    [Fact]
    public async Task Should_Return_Accounts_By_Type()
    {

        int expectedCreditCardTypeAccounts = 2;
        int expectedCashTypeAccounts = 3;

        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearAccounTable(connection);

            for (int i = 0; i < 5; i++)
            {
                Guid newUserId = await CreateNewUser(connection);

                Dictionary<string, object> parameters = GetBaseAccountInsertParameters(i);

                await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters = [new("@Name", parameters["@Name"]),
                                 new("@UserId", newUserId),
                                 new("@BalanceAmount", parameters["@BalanceAmount"]),
                                 new("@CreatedAt", parameters["@CreatedAt"]),
                                 new("@Type", parameters["@Type"]),
                                 new("@Status", parameters["@Status"])
                   ]
                },
                operation: async command =>
                {
                    await command.ExecuteNonQueryAsync();
                },
                default);
            }
            var retrieved = await _accountReadRepository.GetAccountByType(AccountType.CreditCard,connection, default);

            retrieved.Count.Should().Be(expectedCreditCardTypeAccounts);
        });
    }

    private async Task ClearAccounTable(Microsoft.Data.SqlClient.SqlConnection connection)
    {
        await _commandFactory.ExecuteAsync(
                        commandText: $"Delete from {TableName}",
                        connection: connection,
                        options: new CreateSqlCommandOptions(),
                        operation: async command =>
                        {
                            await command.ExecuteNonQueryAsync();
                        },
                        default);
    }
    private async Task<Guid> CreateNewUser(SqlConnection connection)
    {
        var user = new User(
            id: Guid.NewGuid(),
            name: "John Doe",
            email: $"{Guid.NewGuid().ToString().Replace("-", "")}@example.com",
            registeredAt: DateTimeOffset.UtcNow,
            modifiedAt: DateTimeOffset.UtcNow,
            dateOfBirth: DateTimeOffset.UtcNow.AddYears(-30),
            profileImage: "https://example.com/profile.jpg"
        );

        var result = await _userRepository.CreateUserAsync(user, connection);
        return result;
    }
    private static Dictionary<string, object> GetBaseAccountInsertParameters(int pos = 0)
    {
        return new Dictionary<string, object>
        {
            { "@Name", "Test Account" },
            { "@BalanceAmount", 1000.00m },
            { "@CreatedAt", DateTimeOffset.UtcNow },
            { "@Type", pos % 2 == 0 ? AccountType.Cash : AccountType.CreditCard} ,
            { "@Status", pos % 2 == 0 ? AccountStatus.Active : AccountStatus.Closed}
        };
    }
}
