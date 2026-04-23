using FinanceAppDatabase.DbConnection;
using FinancesApp_CQRS.EventStore;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.Events;
using FinancesApp_Module_Account.Domain.ValueObjects;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Tests.Integration.OutboxTests;

public class EventStoreOutboxWorkflowTests : IClassFixture<SqlFixture>
{
    private readonly EventStore _eventStore;
    private readonly IDbConnectionFactory _connectionFactory;

    private record OutboxRow(
        long Id, Guid EventId, Guid AggregateId,
        string EventType, string Payload,
        DateTimeOffset? ProcessedAt, string? Error, int RetryCount);

    public EventStoreOutboxWorkflowTests(SqlFixture fixture)
    {
        _connectionFactory = fixture.ConnectionFactory;
        _eventStore = new EventStore(fixture.CommandFactory, fixture.ConnectionFactory);
    }

    [Fact]
    public async Task Append_Should_Write_To_Both_Events_And_Outbox()
    {
        await ClearTablesAsync();
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);

        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion);

        var eventCount = await GetEventCountAsync(account.Id);
        var outboxRows = await GetOutboxRowsAsync(account.Id);

        eventCount.Should().BeGreaterThan(0);
        outboxRows.Should().HaveCount(eventCount,
            "every event written to the store must also appear in the outbox");
    }

    [Fact]
    public async Task Append_Should_Store_Correct_EventType_AggregateId_And_Payload_In_Outbox()
    {
        await ClearTablesAsync();
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);

        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion);

        var outboxRows = await GetOutboxRowsAsync(account.Id);
        outboxRows.Should().NotBeEmpty();

        var first = outboxRows.First();
        first.AggregateId.Should().Be(account.Id);
        first.EventType.Should().Contain(nameof(AccountCreatedEvent));
        first.Payload.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Append_Should_Create_Outbox_Entry_With_Null_ProcessedAt_And_Zero_RetryCount()
    {
        await ClearTablesAsync();
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);

        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion);

        var outboxRows = await GetOutboxRowsAsync(account.Id);
        outboxRows.Should().AllSatisfy(row =>
        {
            row.ProcessedAt.Should().BeNull();
            row.RetryCount.Should().Be(0);
            row.Error.Should().BeNull();
        });
    }

    [Fact]
    public async Task Append_Should_Throw_ConcurrencyException_On_Version_Mismatch()
    {
        await ClearTablesAsync();
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion);

        // Load the account and make a change — then pass a stale expectedVersion,
        // simulating a writer that read before a concurrent commit and is now behind
        var loaded = new Account();
        loaded.RebuildFromEvents(await _eventStore.Load(account.Id));
        loaded.ApplyDelta(new Money(100m, "USD"));

        var act = async () => await _eventStore.Append(
            loaded.Id, loaded.GetUncommittedEvents(), expectedVersion: 99);

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    [Fact]
    public async Task Concurrent_Appends_With_Same_Expected_Version_Should_Only_Commit_One()
    {
        // Two concurrent writers both read the same CurrentVersion before either commits
        // (ReadCommitted race window). The UNIQUE constraint on (Aggregate_Id, Version)
        // ensures only one INSERT wins; the other is rejected with a SqlException (2627/2601).
        // If the app-level version check fires first (serial interleaving), the loser
        // gets a ConcurrencyException instead. Either way: exactly one succeeds.
        await ClearTablesAsync();

        // Account creation raises AccountCreatedEvent + CalculatedCreditLimitEvent (2 events).
        // Capture initialCount dynamically so the assertion is not sensitive to that detail.
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);
        await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion);
        var initialCount = await GetEventCountAsync(account.Id);

        var writerA = new Account();
        writerA.RebuildFromEvents(await _eventStore.Load(account.Id));
        writerA.ApplyDelta(new Money(100m, "USD"));

        var writerB = new Account();
        writerB.RebuildFromEvents(await _eventStore.Load(account.Id));
        writerB.ApplyDelta(new Money(200m, "USD"));

        // Both carry the same CurrentVersion — fire in parallel, absorb exceptions manually
        var taskA = _eventStore.Append(writerA.Id, writerA.GetUncommittedEvents(), writerA.CurrentVersion);
        var taskB = _eventStore.Append(writerB.Id, writerB.GetUncommittedEvents(), writerB.CurrentVersion);
        await Task.WhenAll(taskA.ContinueWith(_ => { }), taskB.ContinueWith(_ => { }));

        // Exactly one writer must have succeeded and one must have been rejected
        var tasks = new[] { taskA, taskB };
        tasks.Count(t => t.IsCompletedSuccessfully).Should().Be(1,
            "exactly one concurrent writer should commit");
        tasks.Count(t => t.IsFaulted).Should().Be(1,
            "exactly one concurrent writer should be rejected");

        var loserException = tasks.Single(t => t.IsFaulted).Exception!.InnerException;
        loserException.Should().Match<Exception>(ex => ex is ConcurrencyException || ex is SqlException,
            "the losing writer must fail with a ConcurrencyException (app-level check) " +
            "or SqlException 2627/2601 (UNIQUE constraint), not an unrelated error");

        // Final event count must reflect exactly one successful write
        var finalCount = await GetEventCountAsync(account.Id);
        finalCount.Should().Be(initialCount + 1,
            "only one of the two concurrent writers should have persisted its event");
    }

    [Fact]
    public async Task Single_Event_Append_Overload_Should_Write_To_Both_Tables()
    {
        await ClearTablesAsync();
        var account = new Account(Guid.NewGuid(), new Money(1000m, "USD"), AccountType.Checking);
        var singleEvent = account.GetUncommittedEvents().First();

        await _eventStore.Append(account.Id, singleEvent, account.CurrentVersion);

        var eventCount = await GetEventCountAsync(account.Id);
        var outboxRows = await GetOutboxRowsAsync(account.Id);

        eventCount.Should().Be(1);
        outboxRows.Should().HaveCount(1);
        outboxRows.Single().EventId.Should().Be(singleEvent.EventId);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

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

    private async Task<int> GetEventCountAsync(Guid aggregateId)
    {
        return await _connectionFactory.ExecuteInScopeAsync<int>(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM [FinanceApp].[dbo].[Events] WHERE Aggregate_Id = @id";
            cmd.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.UniqueIdentifier) { Value = aggregateId });
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        });
    }

    private async Task<List<OutboxRow>> GetOutboxRowsAsync(Guid aggregateId)
    {
        return await _connectionFactory.ExecuteInScopeAsync<List<OutboxRow>>(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, event_id, aggregate_id, event_type, payload, processed_at, error, retry_count
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
                    EventId:     reader.GetGuid(1),
                    AggregateId: reader.GetGuid(2),
                    EventType:   reader.GetString(3),
                    Payload:     reader.GetString(4),
                    ProcessedAt: reader.IsDBNull(5) ? null : reader.GetDateTimeOffset(5),
                    Error:       reader.IsDBNull(6) ? null : reader.GetString(6),
                    RetryCount:  reader.GetInt32(7)
                ));
            }
            return rows;
        });
    }
}
