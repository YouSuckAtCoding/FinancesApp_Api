using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Module_Credentials.Application.Repositories;
public class UserCredentialsReadRepository : IUserCredentialsReadRepository
{
    private readonly ICommandFactory _commandFactory;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public UserCredentialsReadRepository(IDbConnectionFactory dbConnectionFactory,
                                     ICommandFactory commandFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _commandFactory = commandFactory;
    }

    public async Task<UserCredentials> GetByUserIdAsync(Guid userId,
                                                        SqlConnection? connection = null, 
                                                        CancellationToken token = default)
    {
        const string SelectCommandText = @"SELECT Id, UserId, Login, PasswordHash
                                           FROM [FinanceApp].[dbo].[UserCredentials]
                                           WHERE UserId = @UserId";

        var credentials = await _commandFactory.ExecuteAsync(
            commandText: SelectCommandText,
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new("@UserId", System.Data.SqlDbType.UniqueIdentifier) { Value = userId }]
            },
            operation: async command =>
            {
                await using var reader = await command.ExecuteReaderAsync(token);

                if (!await reader.ReadAsync(token))
                    return new UserCredentials();

                return new UserCredentials(reader.GetGuid(reader.GetOrdinal("Id")),
                                          reader.GetGuid(reader.GetOrdinal("UserId")),
                                          reader.GetString(reader.GetOrdinal("Login")),
                                          reader.GetString(reader.GetOrdinal("PasswordHash")));
            },
            token);

        return credentials;
    }

    public async Task<UserCredentials> GetByLoginAsync(string login,
                                                       SqlConnection? connection = null,
                                                       CancellationToken token = default)
    {
        const string SelectCommandText = @"SELECT Id, UserId, Login, PasswordHash
                                           FROM [FinanceApp].[dbo].[UserCredentials]
                                           WHERE Login = @Login";

        var credentials = await _commandFactory.ExecuteAsync(
            commandText: SelectCommandText,
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new SqlParameter("@Login", login)]
            },
            operation: async command =>
            {
                await using var reader = await command.ExecuteReaderAsync(token);

                if (!await reader.ReadAsync(token))
                    return new UserCredentials();

                return new UserCredentials(reader.GetGuid(reader.GetOrdinal("Id")),
                                           reader.GetGuid(reader.GetOrdinal("UserId")),
                                           reader.GetString(reader.GetOrdinal("Login")),
                                           reader.GetString(reader.GetOrdinal("PasswordHash")));
            },
            token);

        return credentials;
    }
}
