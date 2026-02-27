using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Module_Credentials.Application.Repositories;
public class UserCredentialsRepository : IUserCredentialsRepository
{
    private readonly ICommandFactory _commandFactory;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public UserCredentialsRepository(IDbConnectionFactory dbConnectionFactory,
                                     ICommandFactory commandFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _commandFactory = commandFactory;
    }

    public async Task<Guid> CreateUserCredentialsAsync(UserCredentials credentials,
                                                       SqlConnection? connection = null,
                                                       CancellationToken token = default)
    {
        const string InsertCommandText = @"INSERT INTO [FinanceApp].[dbo].[UserCredentials]
                                            (UserId, Login, PasswordHash)
                                            OUTPUT INSERTED.Id
                                            VALUES
                                            (@UserId, @Login, @PasswordHash)";

        var parameters = new Dictionary<string, object>
        {
            { "@UserId",       credentials.UserId },
            { "@Login",        credentials.Login },
            { "@PasswordHash", credentials.Password }
        };

        var insertedId = await _commandFactory.ExecuteAsync(
            commandText: InsertCommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [.. parameters.Select(p => new SqlParameter(p.Key, p.Value))]
            },
            operation: async command =>
            {
                return (Guid)await command.ExecuteScalarAsync(token);
            },
            token);

        return insertedId;
    }

    public async Task<bool> UpdatePasswordAsync(Guid userId, 
                                                string newPasswordHash,
                                                SqlConnection? connection = null,
                                                CancellationToken token = default)
    {
        const string UpdateCommandText = @"UPDATE [FinanceApp].[dbo].[UserCredentials]
                                           SET PasswordHash = @PasswordHash
                                           WHERE UserId = @UserId";

        var rowsAffected = await _commandFactory.ExecuteAsync(
            commandText: UpdateCommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters =
                [
                    new SqlParameter("@UserId",       userId),
                    new SqlParameter("@PasswordHash", newPasswordHash)
                ]
            },
            operation: async command => await command.ExecuteNonQueryAsync(token),
            token);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteUserCredentialsAsync(Guid userId,
                                                       SqlConnection? connection = null,
                                                       CancellationToken token = default)
    {
        const string DeleteCommandText = @"DELETE FROM [FinanceApp].[dbo].[UserCredentials]
                                           WHERE UserId = @UserId";

        var rowsAffected = await _commandFactory.ExecuteAsync(
            commandText: DeleteCommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new("@UserId", System.Data.SqlDbType.UniqueIdentifier) { Value = userId }]
            },
            operation: async command => await command.ExecuteNonQueryAsync(token),
            token);

        return rowsAffected > 0;
    }
}
