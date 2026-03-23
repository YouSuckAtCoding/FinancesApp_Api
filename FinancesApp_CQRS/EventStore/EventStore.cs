using FinanceAppDatabase.DbConnection;
using FinancesApp_CQRS.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace FinancesApp_CQRS.EventStore;
public class EventStore(ICommandFactory commandFactory,
                        IDbConnectionFactory connectionFactory) : IDisposable, IEventStore
{
    public async Task Append(Guid aggregateId,
                            IReadOnlyList<IDomainEvent> events, 
                            int expectedVersion,
                            CancellationToken token = default)
    {
        if (events.Count == 0) return;

        try
        {
            await connectionFactory.ExecuteInScopeWithTransactionAsync(async (connection, transaction) =>
            {
                var currentVersion = await GetCurrentVersion(aggregateId, connection, transaction);

                if (currentVersion != expectedVersion)
                    throw new ConcurrencyException(
                        $"Concurrency conflict on aggregate {aggregateId}. " +
                        $"Expected version {expectedVersion}, found {currentVersion}.");
             
                for (int i = 0; i < events.Count; i++)
                {
                    var evt = events[i];
                    var version = expectedVersion + (i + 1);
                    await InsertEvent(evt,
                                      aggregateId,
                                      version,
                                      connection,
                                      transaction,
                                      token);
                }

            }, token);
        }
        catch
        {
            throw;
        }
    }
    public async Task<List<IDomainEvent>> Load(Guid aggregateId,
                                               int fromVersion = 0,
                                               CancellationToken token = default)
    {
        const string CommandText = """
                                   SELECT event_type, payload
                                   FROM [FinanceApp].[dbo].[Events]
                                   WHERE Aggregate_Id = @aggregateId
                                     AND version > @fromVersion
                                   ORDER BY version ASC
                                   """;

        return await commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@aggregateId", SqlDbType.UniqueIdentifier) { Value = aggregateId },
                    new SqlParameter("@fromVersion", SqlDbType.Int) { Value = fromVersion }
                ]
            },
            operation: async cmd =>
            {
                var events = new List<IDomainEvent>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var eventType = reader.GetString(0);
                    var payload = reader.GetString(1);
                    events.Add(Deserialize(eventType, payload));
                }
                return events;
            }, token: token);
    }

    public async Task<List<IDomainEvent>> LoadByDateRange(Guid aggregateId,
                                                          DateTimeOffset from,
                                                          DateTimeOffset to,
                                                          CancellationToken token = default)
    {
        const string CommandText = """
            SELECT event_type, payload
            FROM [FinanceApp].[dbo].[Events]
            WHERE Aggregate_Id = @aggregateId
              AND timestamp BETWEEN @from AND @to
            ORDER BY version ASC
            """;
        return await commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@aggregateId", SqlDbType.UniqueIdentifier) { Value = aggregateId },
                    new SqlParameter("@from", SqlDbType.DateTimeOffset) { Value = from },
                    new SqlParameter("@to", SqlDbType.DateTimeOffset) { Value = to }
                ]
            },
            operation: async cmd =>
            {
                var events = new List<IDomainEvent>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var eventType = reader.GetString(0);
                    var payload = reader.GetString(1);
                    events.Add(Deserialize(eventType, payload));
                }
                return events;
            }, token: token);
    }
    public async Task<List<IDomainEvent>> LoadAuditLog(DateTimeOffset from,
                                                       DateTimeOffset to,
                                                       CancellationToken token = default)
    {
        const string CommandText = """
                                   SELECT event_type, payload
                                   FROM [FinanceApp].[dbo].[Events]
                                   WHERE timestamp BETWEEN @from AND @to
                                   ORDER BY timestamp ASC
                                   """;
        return await commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@from", SqlDbType.DateTimeOffset) { Value = from },
                    new SqlParameter("@to", SqlDbType.DateTimeOffset) { Value = to }
                ]
            },
            operation: async cmd =>
            {
                var events = new List<IDomainEvent>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var eventType = reader.GetString(0);
                    var payload = reader.GetString(1);
                    events.Add(Deserialize(eventType, payload));
                }
                return events;
            },
            token: token);

    }
    private async Task InsertEvent(IDomainEvent evt,
                                   Guid aggregateId,
                                   int version,
                                   SqlConnection connection,
                                   SqlTransaction transaction,
                                   CancellationToken token = default)
    {
        const string CommandText = """
            INSERT INTO [FinanceApp].[dbo].[Events] (event_id, Aggregate_Id, event_type, payload, version, timestamp)
            VALUES (@eventId, @accountId, @eventType, @payload, @version, @timestamp)
            """;

        await commandFactory.ExecuteAsync(
            commandText: CommandText,
            connection: connection,
            transaction: transaction,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@eventId", SqlDbType.UniqueIdentifier) { Value = evt.EventId },
                    new SqlParameter("@accountId", SqlDbType.UniqueIdentifier) { Value = aggregateId },
                    new SqlParameter("@eventType", SqlDbType.NVarChar, 255) { Value = evt.GetType().FullName },
                    new SqlParameter("@payload", SqlDbType.NVarChar, -1) { Value = JsonSerializer.Serialize(evt, evt.GetType()) },
                    new SqlParameter("@version", SqlDbType.Int) { Value = version },
                    new SqlParameter("@timestamp", SqlDbType.DateTimeOffset) { Value = evt.Timestamp }
                ]
            },
            operation: async cmd =>
            {
                await cmd.ExecuteNonQueryAsync();
            },
            token: token);
    }


    private async Task<int> GetCurrentVersion(Guid aggregateId,
                                              SqlConnection connection,
                                              SqlTransaction transaction,
                                              CancellationToken token = default)
    {

        const string CommandText = """
                                   SELECT COALESCE(MAX(version), 0)
                                   FROM [FinanceApp].[dbo].[Events]
                                   WHERE Aggregate_Id = @aggregateId
                                   """;
        return await commandFactory.ExecuteAsync(
            connection: connection,
            transaction: transaction,
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@aggregateId", SqlDbType.UniqueIdentifier) { Value = aggregateId }
                ]
            },
            operation: async cmd =>
            {
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            },
            token: token);
    }

    private static IDomainEvent Deserialize(string eventType, string payload)
    {
        var type = ResolveType(eventType);
        return (IDomainEvent)JsonSerializer.Deserialize(payload, type)!;
    }

    private static Type ResolveType(string eventType)
    {
        var type = Type.GetType(eventType);
        if (type is not null) return type;

        type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(eventType))
            .FirstOrDefault(t => t is not null);
        if (type is not null) return type;

        throw new InvalidOperationException(
            $"Cannot resolve event type '{eventType}'. " +
            $"Ensure the assembly containing this event is loaded.");
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
public class ConcurrencyException(string message) : Exception(message)
{
    
};

