using FinanceAppDatabase.DbConnection;
using FinancesApp_CQRS.EventStore;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_CQRS.Outbox;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancesApp_Tests.Integration.OutboxTests;

public class OutboxProcessorTests : IClassFixture<SqlFixture>
{
    private readonly EventStore _eventStore;
    private readonly ICommandFactory _commandFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    // Must match the private const in OutboxProcessor
    private const int MaxRetries = 5;

    public OutboxProcessorTests(SqlFixture fixture)
    {
        _connectionFactory = fixture.ConnectionFactory;
        _commandFactory = fixture.CommandFactory;
        _eventStore = new EventStore(fixture.CommandFactory, fixture.ConnectionFactory);
    }

    private sealed class FakeEventDispatcher : IEventDispatcher
    {
        public List<IDomainEvent> Dispatched { get; } = [];
        public bool ShouldThrow { get; set; }

        public Task Dispatch(IDomainEvent evt, CancellationToken token = default)
        {
            if (ShouldThrow) throw new Exception("Simulated dispatch failure");
            Dispatched.Add(evt);
            return Task.CompletedTask;
        }

        public Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken token = default)
        {
            foreach (var evt in events) Dispatch(evt, token);
            return Task.CompletedTask;
        }

        public void Register<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent { }
    }

    [Fact]
    public async Task Processor_Should_Dispatch_Pending_Entries_And_Mark_Processed()
    {
        await ClearTablesAsync();
        var dispatcher = new FakeEventDispatcher();
        var processor = BuildProcessor(dispatcher);

        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion);

        var ids = await GetOutboxIdsAsync(account.Id);
        ids.Should().NotBeEmpty();

        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);

        await WaitUntilProcessedAsync(ids.First(), timeoutSeconds: 5);

        await cts.CancelAsync();
        await processor.StopAsync(CancellationToken.None);

        dispatcher.Dispatched.Should().NotBeEmpty();
        var rows = await GetOutboxRowsAsync(account.Id);
        rows.Should().AllSatisfy(r => r.ProcessedAt.Should().NotBeNull());
    }

    [Fact]
    public async Task Processor_Should_Not_Redispatch_Already_Processed_Entries()
    {
        await ClearTablesAsync();
        var dispatcher = new FakeEventDispatcher();
        var processor = BuildProcessor(dispatcher);

        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion);
        await MarkAllProcessedAsync(account.Id);

        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();
        await processor.StopAsync(CancellationToken.None);

        dispatcher.Dispatched.Should().BeEmpty("all entries were already marked processed");
    }

    [Fact]
    public async Task Processor_Should_Record_Error_And_Increment_RetryCount_On_Failed_Dispatch()
    {
        await ClearTablesAsync();
        var dispatcher = new FakeEventDispatcher { ShouldThrow = true };
        var processor = BuildProcessor(dispatcher);

        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion);

        var ids = await GetOutboxIdsAsync(account.Id);

        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);
        await WaitUntilRetryCountIncreasedAsync(ids.First(), timeoutSeconds: 5);
        await cts.CancelAsync();
        await processor.StopAsync(CancellationToken.None);

        var rows = await GetOutboxRowsAsync(account.Id);
        rows.Should().AllSatisfy(r =>
        {
            r.RetryCount.Should().BeGreaterThan(0);
            r.Error.Should().NotBeNullOrEmpty();
            r.ProcessedAt.Should().BeNull();
        });
    }

    [Fact]
    public async Task Processor_Should_Not_Fetch_Entries_That_Have_Reached_MaxRetries()
    {
        await ClearTablesAsync();
        var dispatcher = new FakeEventDispatcher();
        var processor = BuildProcessor(dispatcher);

        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion);
        await SetRetryCountAsync(account.Id, MaxRetries);

        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();
        await processor.StopAsync(CancellationToken.None);

        dispatcher.Dispatched.Should().BeEmpty("entries at max retries must be skipped");
        var rows = await GetOutboxRowsAsync(account.Id);
        rows.Should().AllSatisfy(r => r.ProcessedAt.Should().BeNull());
    }

    [Fact]
    public async Task Processor_Should_Not_Dispatch_When_Outbox_Is_Empty()
    {
        await ClearTablesAsync();
        var dispatcher = new FakeEventDispatcher();
        var processor = BuildProcessor(dispatcher);

        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();
        await processor.StopAsync(CancellationToken.None);

        dispatcher.Dispatched.Should().BeEmpty();
    }

    // ─── Infrastructure ──────────────────────────────────────────────────────

    private OutboxProcessor BuildProcessor(IEventDispatcher dispatcher) =>
        new(_commandFactory, dispatcher, NullLogger<OutboxProcessor>.Instance);

    private async Task WaitUntilProcessedAsync(long id, int timeoutSeconds)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = await _connectionFactory.ExecuteInScopeAsync(async conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT processed_at FROM [FinanceApp].[dbo].[Outbox] WHERE id = @id";
                cmd.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.BigInt) { Value = id });
                var val = await cmd.ExecuteScalarAsync();
                return val is DBNull or null ? null : (DateTimeOffset?)val;
            });
            if (result.HasValue) return;
            await Task.Delay(50);
        }
        throw new TimeoutException($"Outbox entry {id} was not marked processed within {timeoutSeconds}s.");
    }

    private async Task WaitUntilRetryCountIncreasedAsync(long id, int timeoutSeconds)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var count = await _connectionFactory.ExecuteInScopeAsync<int>(async conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT retry_count FROM [FinanceApp].[dbo].[Outbox] WHERE id = @id";
                cmd.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.BigInt) { Value = id });
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            });
            if (count > 0) return;
            await Task.Delay(50);
        }
        throw new TimeoutException($"Outbox entry {id} retry_count did not increase within {timeoutSeconds}s.");
    }

    private async Task ClearTablesAsync()
    {
        await _connectionFactory.ExecuteInScopeAsync(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                DELETE FROM [FinanceApp].[dbo].[Outbox];
                DELETE FROM [FinanceApp].[dbo].[Events];
                """;
            await cmd.ExecuteNonQueryAsync();
        });
    }

    private async Task MarkAllProcessedAsync(Guid aggregateId)
    {
        await _connectionFactory.ExecuteInScopeAsync(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE [FinanceApp].[dbo].[Outbox]
                SET processed_at = @now
                WHERE aggregate_id = @id
                """;
            cmd.Parameters.Add(new SqlParameter("@now", System.Data.SqlDbType.DateTimeOffset) { Value = DateTimeOffset.UtcNow });
            cmd.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.UniqueIdentifier) { Value = aggregateId });
            await cmd.ExecuteNonQueryAsync();
        });
    }

    private async Task SetRetryCountAsync(Guid aggregateId, int count)
    {
        await _connectionFactory.ExecuteInScopeAsync(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE [FinanceApp].[dbo].[Outbox]
                SET retry_count = @count
                WHERE aggregate_id = @id
                """;
            cmd.Parameters.Add(new SqlParameter("@count", System.Data.SqlDbType.Int) { Value = count });
            cmd.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.UniqueIdentifier) { Value = aggregateId });
            await cmd.ExecuteNonQueryAsync();
        });
    }

    private async Task<List<long>> GetOutboxIdsAsync(Guid aggregateId)
    {
        return await _connectionFactory.ExecuteInScopeAsync<List<long>>(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id FROM [FinanceApp].[dbo].[Outbox] WHERE aggregate_id = @id ORDER BY id ASC";
            cmd.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.UniqueIdentifier) { Value = aggregateId });
            var ids = new List<long>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) ids.Add(reader.GetInt64(0));
            return ids;
        });
    }

    private record OutboxRow(long Id, DateTimeOffset? ProcessedAt, string? Error, int RetryCount);

    private async Task<List<OutboxRow>> GetOutboxRowsAsync(Guid aggregateId)
    {
        return await _connectionFactory.ExecuteInScopeAsync<List<OutboxRow>>(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, processed_at, error, retry_count
                FROM [FinanceApp].[dbo].[Outbox]
                WHERE aggregate_id = @id
                ORDER BY id ASC
                """;
            cmd.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.UniqueIdentifier) { Value = aggregateId });
            var rows = new List<OutboxRow>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new OutboxRow(
                    Id:          reader.GetInt64(0),
                    ProcessedAt: reader.IsDBNull(1) ? null : reader.GetDateTimeOffset(1),
                    Error:       reader.IsDBNull(2) ? null : reader.GetString(2),
                    RetryCount:  reader.GetInt32(3)
                ));
            }
            return rows;
        });
    }
}
