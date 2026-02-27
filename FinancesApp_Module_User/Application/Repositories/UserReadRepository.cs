using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_User.Domain;
using FinancesAppDatabase.Utils;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FinancesApp_Module_User.Application.Repositories;
public class UserReadRepository : IUserReadRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICommandFactory _commandFactory;

    public UserReadRepository(IDbConnectionFactory connectionFactory, ICommandFactory commandFactory)
    {
        _connectionFactory = connectionFactory;
        _commandFactory = commandFactory;
    }

    public async Task<User> GetUserById(Guid userId,
                                        SqlConnection? connection = null,
                                        CancellationToken token = default)
    {
        try
        {
            return await _commandFactory.ExecuteAsync(
            commandText: @"SELECT                                 
                                [Id],
                                [Name],
                                [Email],
                                [RegisteredAt],
                                [ModifiedAt],
                                [DateOfBirth],
                                [ProfileImage]
                            FROM [FinanceApp].[dbo].[Users]
                            WHERE [Id] = @UserId",
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new("@UserId", SqlDbType.UniqueIdentifier) { Value = userId }]
            },
            operation: async cmd =>
            {
                using var reader = await cmd.ExecuteReaderAsync(token);

                if (await reader.ReadAsync())
                {
                    return new User(
                       reader.GetGuid("Id"),
                       reader.GetString("Name"),
                       reader.GetString("Email"),
                       reader.GetDateTimeOffset("RegisteredAt"),
                       reader.GetDateTimeOffset("ModifiedAt"),
                       reader.GetDateTimeOffset("DateOfBirth"),
                       reader.GetString("ProfileImage")
                    );
                }

                return new User();
            },
            token);
        }
        catch
        {
            throw;
        }
    }
    public async Task<User> GetUserByEmail(string email,
                                           SqlConnection? connection = null,
                                           CancellationToken token = default)
    {
        try
        {
            return await _commandFactory.ExecuteAsync(
                commandText: @"SELECT                                 
                            [Id],
                            [Name],
                            [Email],
                            [RegisteredAt],
                            [ModifiedAt],
                            [DateOfBirth],
                            [ProfileImage]
                        FROM [FinanceApp].[dbo].[Users]
                        WHERE [Email] = @Email",
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters = [new("@Email", SqlDbType.NVarChar, 50) { Value = email }]
                },
                operation: async cmd =>
                {
                    using var reader = await cmd.ExecuteReaderAsync(token);

                    if (await reader.ReadAsync(token))
                    {
                        return new User(
                            reader.GetGuid(reader.GetOrdinal("Id")),
                            reader.GetString(reader.GetOrdinal("Name")),
                            reader.GetString(reader.GetOrdinal("Email")),
                            reader.GetDateTimeOffset(reader.GetOrdinal("RegisteredAt")),
                            reader.GetDateTimeOffset(reader.GetOrdinal("ModifiedAt")),
                            reader.GetDateTimeOffset(reader.GetOrdinal("DateOfBirth")),
                            reader.GetString(reader.GetOrdinal("ProfileImage"))
                        );
                    }

                    return new User();
                },
                token);
        }
        catch
        {
            throw;
        }
    }

    public async Task<IReadOnlyList<User>> GetUsers(SqlConnection? connection = null,
                                                    CancellationToken token = default)
    {
        try
        {
            return await _commandFactory.ExecuteAsync(
                commandText: @"SELECT                                 
                            [Id],
                            [Name],
                            [Email],
                            [RegisteredAt],
                            [ModifiedAt],
                            [DateOfBirth],
                            [ProfileImage]
                        FROM [FinanceApp].[dbo].[Users]
                        ORDER BY [RegisteredAt] DESC",
                connection: connection,
                options: new CreateSqlCommandOptions(),
                operation: async cmd =>
                {
                    var users = new List<User>();
                    using var reader = await cmd.ExecuteReaderAsync(token);

                    while (await reader.ReadAsync(token))
                    {
                        users.Add(new User(
                            reader.GetGuid(reader.GetOrdinal("Id")),
                            reader.GetString(reader.GetOrdinal("Name")),
                            reader.GetString(reader.GetOrdinal("Email")),
                            reader.GetDateTimeOffset(reader.GetOrdinal("RegisteredAt")),
                            reader.GetDateTimeOffset(reader.GetOrdinal("ModifiedAt")),
                            reader.GetDateTimeOffset(reader.GetOrdinal("DateOfBirth")),
                            reader.GetString(reader.GetOrdinal("ProfileImage"))
                        ));
                    }

                    return users;
                },
                token);
        }
        catch
        {
            throw;
        }
    }

    public async Task<IReadOnlyList<User>> GetUsersRegisteredAfter(DateTimeOffset filterDate,
                                                                   SqlConnection? connection = null,
                                                                   CancellationToken token = default)
    {
        try
        {
            return await _commandFactory.ExecuteAsync(
                commandText: @"SELECT                                 
                            [Id],
                            [Name],
                            [Email],
                            [RegisteredAt],
                            [ModifiedAt],
                            [DateOfBirth],
                            [ProfileImage]
                        FROM [FinanceApp].[dbo].[Users]
                        WHERE [RegisteredAt] >= @FilterDate
                        ORDER BY [RegisteredAt] DESC",
                connection: connection,
                options: new CreateSqlCommandOptions
                {
                    Parameters = [new("@FilterDate", SqlDbType.DateTimeOffset) { Value = filterDate }]
                },
                operation: async cmd =>
                {
                    var users = new List<User>();
                    using var reader = await cmd.ExecuteReaderAsync(token);

                    while (await reader.ReadAsync(token))
                    {
                        users.Add(new User(
                            reader.GetGuid(reader.GetOrdinal("Id")),
                            reader.GetString(reader.GetOrdinal("Name")),
                            reader.GetString(reader.GetOrdinal("Email")),
                            reader.GetDateTimeOffset(reader.GetOrdinal("RegisteredAt")),
                            reader.GetDateTimeOffset(reader.GetOrdinal("ModifiedAt")),
                            reader.GetDateTimeOffset(reader.GetOrdinal("DateOfBirth")),
                            reader.GetString(reader.GetOrdinal("ProfileImage"))
                        ));
                    }

                    return users;
                },
                token);
        }
        catch
        {
            throw;
        }
    }
}
