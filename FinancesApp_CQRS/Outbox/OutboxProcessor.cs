using FinanceAppDatabase.DbConnection;
using FinancesApp_CQRS.Interfaces;
using FinancesAppDatabase.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using System.Data;
using System.Text.Json;

namespace FinancesApp_CQRS.Outbox;

public class OutboxProcessor(ICommandFactory commandFactory,
                             IEventDispatcher dispatcher,
                             ILogger<OutboxProcessor> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxRetries = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private static readonly Counter EventsProcessed = Metrics
        .CreateCounter("outbox_events_processed_total", "Total number of [FinanceApp].[dbo].[Outbox] events successfully dispatched");

    private static readonly Counter EventsFailed = Metrics
        .CreateCounter("outbox_events_failed_total", "Total number of [FinanceApp].[dbo].[Outbox] events that failed to dispatch");

    private static readonly Histogram BatchDuration = Metrics
        .CreateHistogram("outbox_batch_duration_seconds", "[FinanceApp].[dbo].[Outbox] batch processing time",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var processed = await ProcessBatchAsync(token);

            if (processed < BatchSize)
                await Task.Delay(PollInterval, token);
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken token)
    {
        var entries = await FetchPendingAsync(token);

        using (BatchDuration.NewTimer())
        {
            foreach (var entry in entries)
            {
                try
                {
                    var evt = Deserialize(entry.EventType, entry.Payload);
                    await dispatcher.Dispatch(evt, token);
                    await MarkProcessedAsync(entry.Id, token);
                    EventsProcessed.Inc();
                }
                catch (Exception ex)
                {
                    EventsFailed.Inc();
                    logger.LogError(ex, "Failed to process [FinanceApp].[dbo].[Outbox] entry {Id}", entry.Id);
                    await RecordErrorAsync(entry.Id, entry.RetryCount, ex.Message, token);

                    if (entry.RetryCount >= MaxRetries)
                        logger.LogCritical("[FinanceApp].[dbo].[Outbox] entry {Id} exceeded max retries. Manual intervention required.", entry.Id);
                }
            }
        }

        return entries.Count;
    }

    private async Task<List<OutboxEntry>> FetchPendingAsync(CancellationToken token)
    {
        const string CommandText = """
        SELECT TOP (@batchSize) id, event_type, payload, retry_count
        FROM [FinanceApp].[dbo].[Outbox] WITH (UPDLOCK, READPAST)
        WHERE processed_at IS NULL
        AND retry_count < @maxRetries
        ORDER BY created_at ASC
        """;

        return await commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@batchSize", SqlDbType.Int) { Value = BatchSize },
                new SqlParameter("@maxRetries", SqlDbType.Int) { Value = MaxRetries }
                ]
            },
            operation: async cmd =>
            {
                var entries = new List<OutboxEntry>();
                using var reader = await cmd.ExecuteReaderAsync(token);
                while (await reader.ReadAsync(token))
                {
                    entries.Add(new OutboxEntry(
                        Id: reader.GetLong("id"),
                        EventType: reader.GetString("event_type"),
                        Payload: reader.GetString("payload"),
                        RetryCount: reader.GetInt("retry_count")
                    ));
                }
                return entries;
            },
            token: token);
    }

    private async Task MarkProcessedAsync(long id, CancellationToken token)
    {
        const string CommandText = """
        UPDATE [FinanceApp].[dbo].[Outbox] SET processed_at = @now WHERE id = @id
        """;

        await commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@now", SqlDbType.DateTimeOffset) { Value = DateTimeOffset.UtcNow },
                new SqlParameter("@id", SqlDbType.BigInt) { Value = id }
                ]
            },
            operation: async cmd => { await cmd.ExecuteNonQueryAsync(); },
            token: token);
    }

    private async Task RecordErrorAsync(long id, int retryCount, string error, CancellationToken token)
    {
        const string CommandText = """
        UPDATE [FinanceApp].[dbo].[Outbox] SET retry_count = @retryCount + 1, error = @error WHERE id = @id
        """;

        await commandFactory.ExecuteAsync(
            commandText: CommandText,
            options: new CreateSqlCommandOptions
            {
                Parameters = [
                    new SqlParameter("@retryCount", SqlDbType.Int) { Value = retryCount },
                new SqlParameter("@error", SqlDbType.NVarChar, -1) { Value = error },
                new SqlParameter("@id", SqlDbType.BigInt) { Value = id }
                ]
            },
            operation: async cmd => { await cmd.ExecuteNonQueryAsync(); },
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
}
