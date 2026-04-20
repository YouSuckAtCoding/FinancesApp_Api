using FinanceAppDatabase.DbConnection;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Module_Credentials.Application.Repositories;
public class UserCredentialsTotpReadRepository : IUserCredentialsTotpReadRepository
{
    private readonly ICommandFactory _commandFactory;

    public UserCredentialsTotpReadRepository(ICommandFactory commandFactory)
    {
        _commandFactory = commandFactory;
    }

    public async Task<UserCredentialsTotp?> GetActiveTotpByUserIdAsync(Guid userId,
                                                                        SqlConnection? connection = null,
                                                                        CancellationToken token = default)
    {
        try
        {
            const string SelectCommandText = @"SELECT Id, UserId, SecurityCode, CreatedAt, InvalidAt, Active
                                           FROM [FinanceApp].[dbo].[UserCredentialsTotp]
                                           WHERE UserId = @UserId AND Active = 1";

            var result = await _commandFactory.ExecuteAsync(
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
                        return null;

                    return new UserCredentialsTotp(
                        id: reader.GetGuid(reader.GetOrdinal("Id")),
                        userId: reader.GetGuid(reader.GetOrdinal("UserId")),
                        securityCode: reader.GetString(reader.GetOrdinal("SecurityCode")),
                        createdAt: reader.GetDateTimeOffset(reader.GetOrdinal("CreatedAt")),
                        invalidAt: reader.GetDateTimeOffset(reader.GetOrdinal("InvalidAt")),
                        active: reader.GetBoolean(reader.GetOrdinal("Active")));
                },
                token);

            return result;
        }
        catch (Exception ex)
        {
            throw;
        }


        
    }
}
