using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;

namespace FinancesApp_Tests.UserCredentialsTests.Repositories;

public class UserCredentialsReadRepositoryTests : IClassFixture<SqlFixture>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICommandFactory _commandFactory;
    private readonly IUserCredentialsReadRepository _userCredentialsReadRepository;
    private readonly IUserRepository _userRepository;
    private readonly SqlFixture _fixture;

    private const string TableName = "[FinanceApp].[dbo].[UserCredentials]";
    private const string InsertCommandText =
        $" INSERT INTO {TableName} (UserId, Login, PasswordHash)" +
        " OUTPUT INSERTED.Id" +
        " VALUES (@UserId, @Login, @PasswordHash)";

    public UserCredentialsReadRepositoryTests(SqlFixture fixture)
    {
        _fixture = fixture;
        _connectionFactory = fixture.ConnectionFactory;
        _commandFactory = fixture.CommandFactory;
        _userCredentialsReadRepository = new UserCredentialsReadRepository(_connectionFactory, _commandFactory);
        _userRepository = new UserRepository(_connectionFactory, _commandFactory);
    }

    [Fact]
    public async Task Should_Reach_TestDatabase_And_Try_To_Retrieve_UserCredentials()
    {
        var result = await _commandFactory.ExecuteAsync(
            commandText: $"SELECT TOP 1 * FROM {TableName}",
            options: new CreateSqlCommandOptions(),
            operation: async command =>
            {
                return await command.ExecuteReaderAsync();
            },
            default);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Return_UserCredentials_By_UserId()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            
            Guid newUserId = await CreateNewUser(connection);

            var parameters = GetBaseCredentialsInsertParameters();

            Guid insertedUserId = newUserId;

            await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters =
                    [
                        new("@UserId",      insertedUserId),
                        new("@Login",       parameters["@Login"]),
                        new("@PasswordHash",parameters["@PasswordHash"])
                    ]
                },
                operation: async command =>
                {
                    await command.ExecuteNonQueryAsync();
                },
                default);

            var retrieved = await _userCredentialsReadRepository.GetByUserIdAsync(insertedUserId, connection, default);

            retrieved.Should().NotBeNull();
            retrieved.UserId.Should().Be(insertedUserId);
        });
    }

 
    [Fact]
    public async Task Should_Return_UserCredentials_By_Login()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {

            Guid newUserId = await CreateNewUser(connection);

            var parameters = GetBaseCredentialsInsertParameters();
            var login = (string)parameters["@Login"];

            await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters =
                    [
                        new("@UserId",      newUserId),
                        new("@Login",       parameters["@Login"]),
                        new("@PasswordHash",parameters["@PasswordHash"])
                    ]
                },
                operation: async command =>
                {
                    await command.ExecuteNonQueryAsync();
                },
                default);

            var retrieved = await _userCredentialsReadRepository.GetByLoginAsync(login, connection, default);

            retrieved.Should().NotBeNull();
            retrieved.Login.Should().Be(login);
        });
    }

    [Fact]
    public async Task Should_Return_Empty_UserCredentials_When_UserId_Not_Found()
    {
        var nonExistentUserId = Guid.NewGuid();

        var retrieved = await _userCredentialsReadRepository.GetByUserIdAsync(nonExistentUserId, default);

        retrieved.Should().NotBeNull();
        retrieved.Should().BeEquivalentTo(new UserCredentials());
    }

    [Fact]
    public async Task Should_Return_Empty_UserCredentials_When_Login_Not_Found()
    {
        var retrieved = await _userCredentialsReadRepository.GetByLoginAsync("nonexistent_login", default);

        retrieved.Should().NotBeNull();
        retrieved.Should().BeEquivalentTo(new UserCredentials());
    }

    [Fact]
    public async Task Should_Return_Correct_All_Fields_When_Retrieved_By_UserId()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            var parameters = GetBaseCredentialsInsertParameters();

            Guid insertedUserId = await CreateNewUser(connection); 

            Guid insertedId = await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters =
                    [
                        new("@UserId",      insertedUserId),
                        new("@Login",       parameters["@Login"]),
                        new("@PasswordHash",parameters["@PasswordHash"])
                    ]
                },
                operation: async command =>
                {
                    var ids = new List<Guid>();
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        ids.Add(reader.GetGuid(0));
                    return ids.First();
                },
                default);

            var retrieved = await _userCredentialsReadRepository.GetByUserIdAsync(insertedUserId, connection, default);

            retrieved.Id.Should().Be(insertedId);
            retrieved.UserId.Should().Be(insertedUserId);
            retrieved.Login.Should().Be((string)parameters["@Login"]);
            retrieved.Password.Should().Be((string)parameters["@PasswordHash"]);
        });
    }

    [Fact]
    public async Task Should_Return_Correct_All_Fields_When_Retrieved_By_Login()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            var parameters = GetBaseCredentialsInsertParameters();

            Guid insertedUserId = await CreateNewUser(connection);

            var login = (string)parameters["@Login"];

            Guid insertedId = await _commandFactory.ExecuteAsync(
                commandText: InsertCommandText,
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters =
                    [
                        new("@UserId",      insertedUserId),
                        new("@Login",       parameters["@Login"]),
                        new("@PasswordHash",parameters["@PasswordHash"])
                    ]
                },
                operation: async command =>
                {
                    var ids = new List<Guid>();
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        ids.Add(reader.GetGuid(0));
                    return ids.First();
                },
                default);

            var retrieved = await _userCredentialsReadRepository.GetByLoginAsync(login, connection, default);

            retrieved.Id.Should().Be(insertedId);
            retrieved.UserId.Should().Be(insertedUserId);
            retrieved.Login.Should().Be(login);
            retrieved.Password.Should().Be((string)parameters["@PasswordHash"]);
        });
    }

    private async Task<Guid> CreateNewUser(Microsoft.Data.SqlClient.SqlConnection connection)
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


    private async Task ClearCredentialsTable(Microsoft.Data.SqlClient.SqlConnection connection)
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

    private async Task ClearUsersTable(Microsoft.Data.SqlClient.SqlConnection connection)
    {
        await _commandFactory.ExecuteAsync(
            commandText: $"DELETE FROM [FinanceApp].[dbo].[Users]",
            connection: connection,
            options: new CreateSqlCommandOptions(),
            operation: async command =>
            {
                await command.ExecuteNonQueryAsync();
            },
            default);
    }

    private static Dictionary<string, object> GetBaseCredentialsInsertParameters()
    {
        return new Dictionary<string, object>
        {
            { "@Login",        $"testuser_{Guid.NewGuid():N}"[..20] },
            { "@PasswordHash", "$2a$11$examplehashedpasswordvalue123456" }
        };
    }
}