using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Tests.UserTests.Repositories;

public class UserRepositoryTests : IClassFixture<SqlFixture>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICommandFactory _commandFactory;
    private readonly IUserRepository _userRepository;

    private const string TableName = "[FinanceApp].[dbo].[Users]";

    public UserRepositoryTests(SqlFixture fixture)
    {
        _connectionFactory = fixture.ConnectionFactory;
        _commandFactory = fixture.CommandFactory;
        _userRepository = new UserRepository(_connectionFactory, _commandFactory);
    }

    [Fact]
    public async Task CreateUserAsync_Should_Insert_User_To_Database()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var user = new User(
                id: Guid.NewGuid(),
                name: "John Doe",
                email: "john.doe@example.com",
                registeredAt: DateTimeOffset.UtcNow,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: DateTimeOffset.UtcNow.AddYears(-30),
                profileImage: "https://example.com/profile.jpg"
            );

            var result = await _userRepository.CreateUserAsync(user);

            result.Should().NotBe(Guid.Empty);

            var retrieved = await GetUserByIdAsync(user.Id, connection);
            retrieved.Should().NotBeNull();
            retrieved.Id.Should().Be(user.Id);
            retrieved.Name.Should().Be("John Doe");
            retrieved.Email.Should().Be("john.doe@example.com");
            retrieved.ProfileImage.Should().Be("https://example.com/profile.jpg");
        });
    }

    [Fact]
    public async Task CreateUserAsync_Should_Insert_User_With_Empty_ProfileImage()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var user = new User(
                id: Guid.NewGuid(),
                name: "Jane Smith",
                email: "jane.smith@example.com",
                registeredAt: DateTimeOffset.UtcNow,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: DateTimeOffset.UtcNow.AddYears(-25),
                profileImage: ""
            );

            var result = await _userRepository.CreateUserAsync(user);

            result.Should().NotBe(Guid.Empty);

            var retrieved = await GetUserByIdAsync(user.Id, connection);
            retrieved.Should().NotBeNull();
            retrieved.ProfileImage.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task CreateUserAsync_Should_Insert_User_With_Different_DateOfBirth()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-40);
            var user = new User(
                id: Guid.NewGuid(),
                name: "Bob Johnson",
                email: "bob.johnson@example.com",
                registeredAt: DateTimeOffset.UtcNow,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: dateOfBirth,
                profileImage: "https://example.com/bob.jpg"
            );

            var result = await _userRepository.CreateUserAsync(user);

            result.Should().NotBe(Guid.Empty);

            var retrieved = await GetUserByIdAsync(user.Id, connection);
            retrieved.DateOfBirth.Should().BeCloseTo(dateOfBirth, TimeSpan.FromSeconds(1));
        });
    }

    [Fact]
    public async Task UpdateUserAsync_Should_Update_User_Name()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var user = new User(
                id: Guid.NewGuid(),
                name: "Original Name",
                email: "original@example.com",
                registeredAt: DateTimeOffset.UtcNow,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: DateTimeOffset.UtcNow.AddYears(-30),
                profileImage: ""
            );

            await _userRepository.CreateUserAsync(user);

            var updatedUser = new User(
                id: user.Id,
                name: "Updated Name",
                email: user.Email,
                registeredAt: user.RegisteredAt,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: user.DateOfBirth,
                profileImage: user.ProfileImage
            );

            var result = await _userRepository.UpdateUserAsync(updatedUser);

            result.Should().BeTrue();

            var retrieved = await GetUserByIdAsync(user.Id, connection);
            retrieved.Name.Should().Be("Updated Name");
        });
    }

    [Fact]
    public async Task UpdateUserAsync_Should_Update_User_Email()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var user = new User(
                id: Guid.NewGuid(),
                name: "Test User",
                email: "old.email@example.com",
                registeredAt: DateTimeOffset.UtcNow,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: DateTimeOffset.UtcNow.AddYears(-28),
                profileImage: ""
            );

            await _userRepository.CreateUserAsync(user);

            var updatedUser = new User(
                id: user.Id,
                name: user.Name,
                email: "new.email@example.com",
                registeredAt: user.RegisteredAt,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: user.DateOfBirth,
                profileImage: user.ProfileImage
            );

            var result = await _userRepository.UpdateUserAsync(updatedUser);

            result.Should().BeTrue();

            var retrieved = await GetUserByIdAsync(user.Id, connection);
            retrieved.Email.Should().Be("new.email@example.com");
        });
    }

    [Fact]
    public async Task UpdateUserAsync_Should_Update_User_ProfileImage()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var user = new User(
                id: Guid.NewGuid(),
                name: "Test User",
                email: "test@example.com",
                registeredAt: DateTimeOffset.UtcNow,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: DateTimeOffset.UtcNow.AddYears(-35),
                profileImage: "https://example.com/old.jpg"
            );

            await _userRepository.CreateUserAsync(user);

            var updatedUser = new User(
                id: user.Id,
                name: user.Name,
                email: user.Email,
                registeredAt: user.RegisteredAt,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: user.DateOfBirth,
                profileImage: "https://example.com/new.jpg"
            );

            var result = await _userRepository.UpdateUserAsync(updatedUser);

            result.Should().BeTrue();

            var retrieved = await GetUserByIdAsync(user.Id, connection);
            retrieved.ProfileImage.Should().Be("https://example.com/new.jpg");
        });
    }

    [Fact]
    public async Task UpdateUserAsync_Should_Update_ModifiedAt_Timestamp()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var user = new User(
                id: Guid.NewGuid(),
                name: "Test User",
                email: "test@example.com",
                registeredAt: DateTimeOffset.UtcNow,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: DateTimeOffset.UtcNow.AddYears(-30),
                profileImage: ""
            );

            await _userRepository.CreateUserAsync(user);
            var originalModifiedAt = user.ModifiedAt;

            await Task.Delay(100);

            var updatedUser = new User(
                id: user.Id,
                name: "Updated Name",
                email: user.Email,
                registeredAt: user.RegisteredAt,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: user.DateOfBirth,
                profileImage: user.ProfileImage
            );

            var result = await _userRepository.UpdateUserAsync(updatedUser);

            result.Should().BeTrue();

            var retrieved = await GetUserByIdAsync(user.Id, connection);
            retrieved.ModifiedAt.Should().BeAfter(originalModifiedAt);
        });
    }

    [Fact]
    public async Task UpdateUserAsync_Should_Return_False_When_User_Does_Not_Exist()
    {
        var nonExistentUser = new User(
            id: Guid.NewGuid(),
            name: "Non-existent User",
            email: "nonexistent@example.com",
            registeredAt: DateTimeOffset.UtcNow,
            modifiedAt: DateTimeOffset.UtcNow,
            dateOfBirth: DateTimeOffset.UtcNow.AddYears(-30),
            profileImage: ""
        );

        var result = await _userRepository.UpdateUserAsync(nonExistentUser);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_Should_Remove_User_From_Database()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var user = new User(
                id: Guid.NewGuid(),
                name: "User To Delete",
                email: "delete@example.com",
                registeredAt: DateTimeOffset.UtcNow,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: DateTimeOffset.UtcNow.AddYears(-30),
                profileImage: ""
            );

            await _userRepository.CreateUserAsync(user);

            var result = await _userRepository.DeleteUserAsync(user.Id);

            result.Should().BeTrue();

            var retrieved = await GetUserByIdAsync(user.Id, connection);
            retrieved.Id.Should().Be(Guid.Empty);
        });
    }

    [Fact]
    public async Task DeleteUserAsync_Should_Return_False_When_User_Does_Not_Exist()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _userRepository.DeleteUserAsync(nonExistentId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUserAsync_Should_Handle_Multiple_Users()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var users = new List<User>
            {
                new User(Guid.NewGuid(), "User 1", "user1@example.com", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(-25), ""),
                new User(Guid.NewGuid(), "User 2", "user2@example.com", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(-30), ""),
                new User(Guid.NewGuid(), "User 3", "user3@example.com", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(-35), "")
            };

            foreach (var user in users)
            {
                var result = await _userRepository.CreateUserAsync(user);
                result.Should().NotBe(Guid.Empty);
            }

            var allUsers = await GetAllUsersAsync(connection);
            allUsers.Count.Should().Be(3);
        });
    }

    [Fact]
    public async Task UpdateUserAsync_Should_Preserve_Immutable_Fields()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var user = new User(
                id: Guid.NewGuid(),
                name: "Test User",
                email: "test@example.com",
                registeredAt: DateTimeOffset.UtcNow,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: DateTimeOffset.UtcNow.AddYears(-30),
                profileImage: ""
            );

            await _userRepository.CreateUserAsync(user);
            var originalRegisteredAt = user.RegisteredAt;

            var updatedUser = new User(
                id: user.Id,
                name: "Updated Name",
                email: user.Email,
                registeredAt: user.RegisteredAt,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: user.DateOfBirth,
                profileImage: user.ProfileImage
            );

            await _userRepository.UpdateUserAsync(updatedUser);

            var retrieved = await GetUserByIdAsync(user.Id, connection);
            retrieved.RegisteredAt.Should().BeCloseTo(originalRegisteredAt, TimeSpan.FromSeconds(1));
        });
    }

    [Fact]
    public async Task UpdateUserAsync_Should_Update_DateOfBirth()
    {
        await _connectionFactory.ExecuteInScopeAsync(async connection =>
        {
            await ClearUserTable(connection);

            var user = new User(
                id: Guid.NewGuid(),
                name: "Test User",
                email: "test@example.com",
                registeredAt: DateTimeOffset.UtcNow,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: DateTimeOffset.UtcNow.AddYears(-30),
                profileImage: ""
            );

            await _userRepository.CreateUserAsync(user);

            var newDateOfBirth = DateTimeOffset.UtcNow.AddYears(-35);
            var updatedUser = new User(
                id: user.Id,
                name: user.Name,
                email: user.Email,
                registeredAt: user.RegisteredAt,
                modifiedAt: DateTimeOffset.UtcNow,
                dateOfBirth: newDateOfBirth,
                profileImage: user.ProfileImage
            );

            var result = await _userRepository.UpdateUserAsync(updatedUser);

            result.Should().BeTrue();

            var retrieved = await GetUserByIdAsync(user.Id, connection);
            retrieved.DateOfBirth.Should().BeCloseTo(newDateOfBirth, TimeSpan.FromSeconds(1));
        });
    }

    private async Task<User> GetUserByIdAsync(Guid userId, SqlConnection connection)
    {
        const string selectCommandText = @"SELECT Id, Name, Email, RegisteredAt, ModifiedAt, DateOfBirth, ProfileImage
                                          FROM [FinanceApp].[dbo].[Users]
                                          WHERE Id = @Id";

        return await _commandFactory.ExecuteAsync(
            commandText: selectCommandText,
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new SqlParameter("@Id", userId)]
            },
            operation: async command =>
            {
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new User(
                        id: reader.GetGuid(reader.GetOrdinal("Id")),
                        name: reader.GetString(reader.GetOrdinal("Name")),
                        email: reader.GetString(reader.GetOrdinal("Email")),
                        registeredAt: reader.GetDateTimeOffset(reader.GetOrdinal("RegisteredAt")),
                        modifiedAt: reader.GetDateTimeOffset(reader.GetOrdinal("ModifiedAt")),
                        dateOfBirth: reader.GetDateTimeOffset(reader.GetOrdinal("DateOfBirth")),
                        profileImage: reader.GetString(reader.GetOrdinal("ProfileImage"))
                    );
                }

                return new User();
            },
            default);
    }

    private async Task<List<User>> GetAllUsersAsync(SqlConnection connection)
    {
        const string selectCommandText = @"SELECT Id, Name, Email, RegisteredAt, ModifiedAt, DateOfBirth, ProfileImage
                                          FROM [FinanceApp].[dbo].[Users]";

        return await _commandFactory.ExecuteAsync(
            commandText: selectCommandText,
            connection: connection,
            options: new CreateSqlCommandOptions(),
            operation: async command =>
            {
                var users = new List<User>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    users.Add(new User(
                        id: reader.GetGuid(reader.GetOrdinal("Id")),
                        name: reader.GetString(reader.GetOrdinal("Name")),
                        email: reader.GetString(reader.GetOrdinal("Email")),
                        registeredAt: reader.GetDateTimeOffset(reader.GetOrdinal("RegisteredAt")),
                        modifiedAt: reader.GetDateTimeOffset(reader.GetOrdinal("ModifiedAt")),
                        dateOfBirth: reader.GetDateTimeOffset(reader.GetOrdinal("DateOfBirth")),
                        profileImage: reader.GetString(reader.GetOrdinal("ProfileImage"))
                    ));
                }

                return users;
            },
            default);
    }

    private async Task ClearUserTable(SqlConnection connection)
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
}
