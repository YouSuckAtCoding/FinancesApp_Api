using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;

namespace FinancesApp_Tests.UserTests.Repositories;

public class UserReadRepositoryTests : IClassFixture<SqlFixture>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICommandFactory _commandFactory;
    private readonly IUserReadRepository _userReadRepository;
    private readonly SqlFixture _fixture;

    private const string TableName = "[FinanceApp].[dbo].[Users]";
    private const string InsertCommandText =
        $" INSERT INTO {TableName} (Id, Name, Email, RegisteredAt, ModifiedAt, DateOfBirth, ProfileImage)" +
        " OUTPUT INSERTED.Id " +
        " VALUES (@Id, @Name, @Email, @RegisteredAt, @ModifiedAt, @DateOfBirth, @ProfileImage)";

    public UserReadRepositoryTests(SqlFixture fixture)
    {
        _connectionFactory = fixture.ConnectionFactory;
        _commandFactory = fixture.CommandFactory;
        _userReadRepository = new UserReadRepository(_connectionFactory, _commandFactory);
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Reach_TestDatabase_And_Try_To_Retrieve_A_User()
    {
        var result = await _commandFactory.ExecuteAsync(
            commandText: $"Select TOP 1 * from {TableName}",
            options: new CreateSqlCommandOptions(),
            operation: async command =>
            {
                return await command.ExecuteReaderAsync();
            },
            default);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Insert_User_To_Database()
    {
        Dictionary<string, object> parameters = GetBaseUserInsertParameters();

        var rowsAffected = await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            return await _commandFactory.ExecuteAsync(
            commandText: InsertCommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new("@Id", parameters["@Id"]),
                   new("@Name", parameters["@Name"]),
                   new("@Email", parameters["@Email"]),
                   new("@RegisteredAt", parameters["@RegisteredAt"]),
                   new("@ModifiedAt", parameters["@ModifiedAt"]),
                   new("@DateOfBirth", parameters["@DateOfBirth"]),
                   new("@ProfileImage", parameters["@ProfileImage"])
                ]
            },
            operation: async command =>
            {
                return await command.ExecuteNonQueryAsync();
            },
            default);
        });

        rowsAffected.Should().Be(1);
    }

    [Fact]
    public async Task Should_Return_User_By_Id()
    {
        Dictionary<string, object> parameters = GetBaseUserInsertParameters();

        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            Guid insertedId = await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters = [
                        new("@Id", parameters["@Id"]),
                        new("@Name", parameters["@Name"]),
                        new("@Email", parameters["@Email"]),
                        new("@RegisteredAt", parameters["@RegisteredAt"]),
                        new("@ModifiedAt", parameters["@ModifiedAt"]),
                        new("@DateOfBirth", parameters["@DateOfBirth"]),
                        new("@ProfileImage", parameters["@ProfileImage"])
                    ]
                },
                operation: async command =>
                {
                    var userIds = new List<Guid>();
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                        userIds.Add(reader.GetGuid(0));

                    return userIds.First();
                },
                default);

            var retrieved = await _userReadRepository.GetUserById(insertedId, connection, default);

            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(insertedId);
            retrieved.Name.Should().Be((string)parameters["@Name"]);
            retrieved.Email.Should().Be((string)parameters["@Email"]);
        });
    }

    [Fact]
    public async Task Should_Return_Null_When_User_Not_Found()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            var nonExistentId = Guid.NewGuid();

            var retrieved = await _userReadRepository.GetUserById(nonExistentId, connection, default);

            retrieved.Should().BeEquivalentTo(new User());
        });
    }

    [Fact]
    public async Task Should_Return_All_Users()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            for (int i = 0; i < 5; i++)
            {
                Dictionary<string, object> parameters = GetBaseUserInsertParameters(i);

                await _commandFactory.ExecuteAsync(
                    commandText: InsertCommandText,
                    connection: connection,
                    options: new CreateSqlCommandOptions
                    {
                        Parameters = [
                            new("@Id", parameters["@Id"]),
                            new("@Name", parameters["@Name"]),
                            new("@Email", parameters["@Email"]),
                            new("@RegisteredAt", parameters["@RegisteredAt"]),
                            new("@ModifiedAt", parameters["@ModifiedAt"]),
                            new("@DateOfBirth", parameters["@DateOfBirth"]),
                            new("@ProfileImage", parameters["@ProfileImage"])
                        ]
                    },
                    operation: async command =>
                    {
                        await command.ExecuteNonQueryAsync();
                    },
                    default);
            }

            var retrieved = await _userReadRepository.GetUsers(connection, default);

            retrieved.Count.Should().Be(5);
        });
    }

    [Fact]
    public async Task Should_Return_User_By_Email()
    {
        Dictionary<string, object> parameters = GetBaseUserInsertParameters();

        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters = [
                        new("@Id", parameters["@Id"]),
                        new("@Name", parameters["@Name"]),
                        new("@Email", parameters["@Email"]),
                        new("@RegisteredAt", parameters["@RegisteredAt"]),
                        new("@ModifiedAt", parameters["@ModifiedAt"]),
                        new("@DateOfBirth", parameters["@DateOfBirth"]),
                        new("@ProfileImage", parameters["@ProfileImage"])
                    ]
                },
                operation: async command =>
                {
                    await command.ExecuteNonQueryAsync();
                },
                default);

            var retrieved = await _userReadRepository.GetUserByEmail((string)parameters["@Email"], connection, default);

            retrieved.Should().NotBeNull();
            retrieved!.Email.Should().Be((string)parameters["@Email"]);
            retrieved.Name.Should().Be((string)parameters["@Name"]);
        });
    }

    [Fact]
    public async Task Should_Return_Users_Registered_After_Date()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var baseDate = DateTimeOffset.UtcNow.AddMonths(-6);

            for (int i = 0; i < 5; i++)
            {
                Dictionary<string, object> parameters = GetBaseUserInsertParameters(i);
                parameters["@RegisteredAt"] = baseDate.AddMonths(i);

                await _commandFactory.ExecuteAsync(
                    commandText: InsertCommandText,
                    connection: connection,
                    options: new CreateSqlCommandOptions
                    {
                        Parameters = [
                            new("@Id", parameters["@Id"]),
                            new("@Name", parameters["@Name"]),
                            new("@Email", parameters["@Email"]),
                            new("@RegisteredAt", parameters["@RegisteredAt"]),
                            new("@ModifiedAt", parameters["@ModifiedAt"]),
                            new("@DateOfBirth", parameters["@DateOfBirth"]),
                            new("@ProfileImage", parameters["@ProfileImage"])
                        ]
                    },
                    operation: async command =>
                    {
                        await command.ExecuteNonQueryAsync();
                    },
                    default);
            }

            var filterDate = baseDate.AddMonths(2);
            var retrieved = await _userReadRepository.GetUsersRegisteredAfter(filterDate, connection, default);

            retrieved.Count.Should().Be(3);
        });
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Users_Exist()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var retrieved = await _userReadRepository.GetUsers(connection, default);

            retrieved.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task Should_Handle_Users_With_Empty_ProfileImage()
    {
        Dictionary<string, object> parameters = GetBaseUserInsertParameters();
        parameters["@ProfileImage"] = "";

        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            Guid insertedId = await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters = [
                        new("@Id", parameters["@Id"]),
                        new("@Name", parameters["@Name"]),
                        new("@Email", parameters["@Email"]),
                        new("@RegisteredAt", parameters["@RegisteredAt"]),
                        new("@ModifiedAt", parameters["@ModifiedAt"]),
                        new("@DateOfBirth", parameters["@DateOfBirth"]),
                        new("@ProfileImage", parameters["@ProfileImage"])
                    ]
                },
                operation: async command =>
                {
                    var userIds = new List<Guid>();
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                        userIds.Add(reader.GetGuid(0));

                    return userIds.First();
                },
                default);

            var retrieved = await _userReadRepository.GetUserById(insertedId, connection, default);

            retrieved.Should().NotBeNull();
            retrieved!.ProfileImage.Should().BeEmpty();
        });
    }

    private async Task ClearUserTable(Microsoft.Data.SqlClient.SqlConnection connection)
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

    private static Dictionary<string, object> GetBaseUserInsertParameters(int pos = 0)
    {
        return new Dictionary<string, object>
        {
            { "@Id", Guid.NewGuid() },
            { "@Name", $"Test User {pos}" },
            { "@Email", $"testuser{pos}@example.com" },
            { "@RegisteredAt", DateTimeOffset.UtcNow },
            { "@ModifiedAt", DateTimeOffset.UtcNow },
            { "@DateOfBirth", DateTimeOffset.UtcNow.AddYears(-25) },
            { "@ProfileImage", $"https://example.com/profile{pos}.jpg" }
        };
    }
}