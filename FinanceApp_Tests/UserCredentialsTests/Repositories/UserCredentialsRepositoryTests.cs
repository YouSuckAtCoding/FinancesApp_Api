using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Tests.UserCredentialsTests.Repositories;

public class UserCredentialsRepositoryTests : IClassFixture<SqlFixture>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICommandFactory _commandFactory;
    private readonly IUserCredentialsRepository _userCredentialsRepository;
    private readonly IUserRepository _userRepository;

    private const string TableName = "[FinanceApp].[dbo].[UserCredentials]";

    public UserCredentialsRepositoryTests(SqlFixture fixture)
    {
        _connectionFactory = fixture.ConnectionFactory;
        _commandFactory = fixture.CommandFactory;
        _userCredentialsRepository = new UserCredentialsRepository(_connectionFactory, _commandFactory);
        _userRepository = new UserRepository(_connectionFactory, _commandFactory);
    }

    [Fact]
    public async Task CreateUserCredentialsAsync_Should_Insert_Credentials_To_Database()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearCredentialsTable(connection);

            var credentials = await BuildCredentials(connection);

            var insertedId = await _userCredentialsRepository.CreateUserCredentialsAsync(credentials);

            var retrieved = await GetCredentialsByUserIdAsync(credentials.UserId, connection);

            retrieved.Should().NotBeNull();
            retrieved.Id.Should().NotBe(Guid.Empty);
            retrieved.UserId.Should().Be(credentials.UserId);
            retrieved.Login.Should().Be(credentials.Login);
            retrieved.Password.Should().Be(credentials.Password);
        });
    }

    [Fact]
    public async Task CreateUserCredentialsAsync_Should_Handle_Multiple_Credentials()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearCredentialsTable(connection);

            var credentialsList = new List<UserCredentials>
            {
                await BuildCredentials(connection),
                await BuildCredentials(connection),
                await BuildCredentials(connection)
            };

            foreach (var credentials in credentialsList)
                await _userCredentialsRepository.CreateUserCredentialsAsync(credentials);           

            var allCredentials = await GetAllCredentialsAsync(connection);
            allCredentials.Count.Should().Be(3);
        });
    }

    [Fact]
    public async Task UpdatePasswordAsync_Should_Update_Password_Hash()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearCredentialsTable(connection);

            var credentials = await BuildCredentials(connection);
            await _userCredentialsRepository.CreateUserCredentialsAsync(credentials);

            var newPasswordHash = "$2a$11$updatedhashedpasswordvalue12345";

            var result = await _userCredentialsRepository.UpdatePasswordAsync(credentials.UserId, newPasswordHash);

            result.Should().BeTrue();

            var retrieved = await GetCredentialsByUserIdAsync(credentials.UserId, connection);
            retrieved.Password.Should().Be(newPasswordHash);
        });
    }

    [Fact]
    public async Task UpdatePasswordAsync_Should_Preserve_Other_Fields()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearCredentialsTable(connection);

            var credentials = await BuildCredentials(connection);
            await _userCredentialsRepository.CreateUserCredentialsAsync(credentials);

            var newPasswordHash = "$2a$11$updatedhashedpasswordvalue12345";

            await _userCredentialsRepository.UpdatePasswordAsync(credentials.UserId, newPasswordHash);

            var retrieved = await GetCredentialsByUserIdAsync(credentials.UserId, connection);
            retrieved.Id.Should().NotBe(Guid.Empty);
            retrieved.UserId.Should().Be(credentials.UserId);
            retrieved.Login.Should().Be(credentials.Login);
        });
    }

    [Fact]
    public async Task UpdatePasswordAsync_Should_Return_False_When_UserId_Does_Not_Exist()
    {
        var nonExistentUserId = Guid.NewGuid();
        var newPasswordHash = "$2a$11$updatedhashedpasswordvalue12345";

        var result = await _userCredentialsRepository.UpdatePasswordAsync(nonExistentUserId, newPasswordHash);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserCredentialsAsync_Should_Remove_Credentials_From_Database()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearCredentialsTable(connection);

            var credentials = await BuildCredentials(connection);
            await _userCredentialsRepository.CreateUserCredentialsAsync(credentials);

            var result = await _userCredentialsRepository.DeleteUserCredentialsAsync(credentials.UserId);

            result.Should().BeTrue();

            var retrieved = await GetCredentialsByUserIdAsync(credentials.UserId, connection);
            retrieved.Id.Should().Be(Guid.Empty);
        });
    }

    [Fact]
    public async Task DeleteUserCredentialsAsync_Should_Return_False_When_UserId_Does_Not_Exist()
    {
        var nonExistentUserId = Guid.NewGuid();

        var result = await _userCredentialsRepository.DeleteUserCredentialsAsync(nonExistentUserId);

        result.Should().BeFalse();
    }


    private async Task<UserCredentials> GetCredentialsByUserIdAsync(Guid userId, SqlConnection connection)
    {
        const string selectCommandText = @"SELECT Id, UserId, Login, PasswordHash
                    FROM [FinanceApp].[dbo].[UserCredentials]
                    WHERE UserId = @UserId";

        return await _commandFactory.ExecuteAsync(
            commandText: selectCommandText,
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new SqlParameter("@UserId", userId)]
            },
            operation: async command =>
            {
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new UserCredentials(
                        reader.GetGuid(reader.GetOrdinal("Id")),
                        reader.GetGuid(reader.GetOrdinal("UserId")),
                        reader.GetString(reader.GetOrdinal("Login")),
                        reader.GetString(reader.GetOrdinal("PasswordHash")));
                }

                return new UserCredentials();
            },
            default);
    }

    private async Task<List<UserCredentials>> GetAllCredentialsAsync(SqlConnection connection)
    {
        const string selectCommandText = @"SELECT Id, UserId, Login, PasswordHash
                    FROM [FinanceApp].[dbo].[UserCredentials]";

        return await _commandFactory.ExecuteAsync(
            commandText: selectCommandText,
            connection: connection,
            options: new CreateSqlCommandOptions(),
            operation: async command =>
            {
                var list = new List<UserCredentials>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new UserCredentials(
                        reader.GetGuid(reader.GetOrdinal("Id")),
                        reader.GetGuid(reader.GetOrdinal("UserId")),
                        reader.GetString(reader.GetOrdinal("Login")),
                        reader.GetString(reader.GetOrdinal("PasswordHash"))));
                }

                return list;
            },
            default);
    }

    private async Task ClearCredentialsTable(SqlConnection connection)
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

    private async Task<UserCredentials> BuildCredentials(SqlConnection connection)
    {
        Guid newUserId = await CreateNewUser(connection);

        return new UserCredentials(
             userId: newUserId,
             login: $"usr_{Guid.NewGuid():N}"[..15],
             passwordHash: "$2a$11$examplehashedpasswordvalue12345"
         );
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
}