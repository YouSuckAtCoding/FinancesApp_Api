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

        try
        {
            return await _dbConnectionFactory.ExecuteInScopeAsync(async connection =>
            {
                var parameters = new Dictionary<string, object>
                   {
                   { "@UserId",       credentials.UserId },
                   { "@Login",        credentials.Email },
                   { "@PasswordHash", credentials.Password }
                   };

                var insertedId = await _commandFactory.ExecuteAsync(
                    commandText: InsertCommandText,
                    connection: connection,
                    options: new CreateSqlCommandOptions
                    {
                        Parameters = [.. parameters.Select(p => new SqlParameter(p.Key, p.Value))]
                    },
                    operation: async command =>
                    {
                        return (Guid)await command.ExecuteScalarAsync(token);
                    },
                    token);

                await UpdateUserSetCredentialsDate(insertedId,
                                                   connection,
                                                   token);

                return insertedId;
            }, token: token);
        }
        catch
        {
            throw;
        }

    }

    public async Task<bool> SaveUserCredentialTotpAsync(UserCredentialsTotp totpCredentials,
                                                        SqlConnection? connection = null,
                                                        CancellationToken token = default)
    {
        const string InsertCommandText = @"INSERT INTO [FinanceApp].[dbo].[UserCredentialsTotp]
                                            (Id, UserId, SecurityCode, CreatedAt, InvalidAt, Active)
                                            VALUES
                                            (@Id, @UserId, @SecurityCode, @CreatedAt, @InvalidAt, @Active)";

        var rowsAffected = await _commandFactory.ExecuteAsync(
            commandText: InsertCommandText,
            connection: connection,
            options: new CreateSqlCommandOptions
            {
                Parameters =
                [
                    new SqlParameter("@Id",           totpCredentials.Id),
                    new SqlParameter("@UserId",       totpCredentials.UserId),
                    new SqlParameter("@SecurityCode", totpCredentials.SecurityCode),
                    new SqlParameter("@CreatedAt",    totpCredentials.CreatedAt),
                    new SqlParameter("@InvalidAt",    totpCredentials.InvalidAt),
                    new SqlParameter("@Active",       totpCredentials.Active)
                ]
            },
            operation: async command => await command.ExecuteNonQueryAsync(token),
            token);

        return rowsAffected > 0;
    }

    public async Task<bool> InvalidateUserCredentialTotpByIdAsync(Guid totpId,
                                                                   SqlConnection? connection = null,
                                                                   CancellationToken token = default)
    {
        const string UpdateCommandText = @"UPDATE [FinanceApp].[dbo].[UserCredentialsTotp]
                                           SET Active = 0
                                           WHERE Id = @Id AND Active = 1";

        var rowsAffected = await _commandFactory.ExecuteAsync(
            commandText: UpdateCommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = totpId }]
            },
            operation: async command => await command.ExecuteNonQueryAsync(token),
            token);

        return rowsAffected > 0;
    }

    public async Task<bool> InvalidateUserCredentialTotpByUserIdAsync(Guid userId,
                                                                       SqlConnection? connection = null,
                                                                       CancellationToken token = default)
    {
        const string UpdateCommandText = @"UPDATE [FinanceApp].[dbo].[UserCredentialsTotp]
                                           SET Active = 0
                                           WHERE UserId = @UserId AND Active = 1";

        var rowsAffected = await _commandFactory.ExecuteAsync(
            commandText: UpdateCommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [new("@UserId", System.Data.SqlDbType.UniqueIdentifier) { Value = userId }]
            },
            operation: async command => await command.ExecuteNonQueryAsync(token),
            token);

        return rowsAffected > 0;
    }

    public async Task UpdateUserSetCredentialsDate(Guid insertedId,
                                                   SqlConnection? connection = null,
                                                   CancellationToken token = default)
    {
        const string UpdateCommandText = @"UPDATE [FinanceApp].[dbo].[Users]
                                           SET CredentialsSetAt = SYSDATETIMEOFFSET()
                                           WHERE Id = @UserId";

        await _commandFactory.ExecuteAsync(
               commandText: UpdateCommandText,
               connection: connection,
               options: new CreateSqlCommandOptions
               {
                   Parameters = [new SqlParameter("@UserId", insertedId)]
               },
               operation: async command =>
               {
                  await command.ExecuteNonQueryAsync(token);
               },
               token);
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
