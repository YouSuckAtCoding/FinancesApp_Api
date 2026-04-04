using FinanceAppDatabase.DbConnection;
using FinancesApp_CQRS.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FinancesApp_CQRS.Projections;

public class ProjectionCheckpoint(ICommandFactory commandFactory) : IProjectionCheckpoint
{
    public async Task<bool> TryClaimAsync(Guid eventId, CancellationToken token = default)
    {
        const string CommandText = """
            INSERT INTO [FinanceApp].[dbo].[ProjectionCheckpoint] (event_id, applied_at)
            VALUES (@eventId, SYSDATETIMEOFFSET())
            """;

        try
        {
            await commandFactory.ExecuteAsync(
                commandText: CommandText,
                options: new CreateSqlCommandOptions
                {
                    Parameters =
                    [
                        new SqlParameter("@eventId", SqlDbType.UniqueIdentifier) { Value = eventId }
                    ]
                },
                operation: async cmd => { await cmd.ExecuteNonQueryAsync(token); },
                token: token);

            return true;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            return false;
        }
    }
}
