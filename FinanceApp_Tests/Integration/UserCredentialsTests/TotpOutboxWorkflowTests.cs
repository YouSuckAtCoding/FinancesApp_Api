using FinanceAppDatabase.DbConnection;
using FinancesApp_CQRS.EventStore;
using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_Credentials.Domain.Events;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace FinancesApp_Tests.Integration.UserCredentialsTests;

public class TotpOutboxWorkflowTests : IClassFixture<SqlFixture>
{
    private readonly EventStore _eventStore;
    private readonly IDbConnectionFactory _connectionFactory;

    private record OutboxRow(
        long Id, Guid EventId, Guid AggregateId,
        string EventType, string Payload,
        DateTimeOffset? ProcessedAt, string? Error, int RetryCount);

    public TotpOutboxWorkflowTests(SqlFixture fixture)
    {
        _connectionFactory = fixture.ConnectionFactory;
        _eventStore = new EventStore(fixture.CommandFactory, fixture.ConnectionFactory);
    }

    [Fact]
    public async Task TotpCreated_Should_Write_To_Both_Events_And_Outbox()
    {
        await ClearTablesAsync();
        var userId = Guid.NewGuid();
        var totp = new UserCredentialsTotp(userId, "JBSWY3DPEHPK3PXP");

        await _eventStore.Append(userId, totp.GetUncommittedEvents(), totp.CurrentVersion);

        var eventCount = await GetEventCountAsync(userId);
        var outboxRows = await GetOutboxRowsAsync(userId);

        eventCount.Should().Be(1);
        outboxRows.Should().HaveCount(1);
    }

    [Fact]
    public async Task TotpCreated_Should_Store_Correct_EventType_And_Payload_In_Outbox()
    {
        await ClearTablesAsync();
        var userId = Guid.NewGuid();
        var totp = new UserCredentialsTotp(userId, "JBSWY3DPEHPK3PXP");

        await _eventStore.Append(userId, totp.GetUncommittedEvents(), totp.CurrentVersion);

        var outboxRows = await GetOutboxRowsAsync(userId);
        outboxRows.Should().ContainSingle();

        var row = outboxRows.Single();
        row.AggregateId.Should().Be(userId);
        row.EventType.Should().Contain(nameof(TotpCredentialCreatedEvent));
        row.Payload.Should().Contain("JBSWY3DPEHPK3PXP");
    }

    [Fact]
    public async Task TotpCreated_Outbox_Should_Have_Null_ProcessedAt_And_Zero_RetryCount()
    {
        await ClearTablesAsync();
        var userId = Guid.NewGuid();
        var totp = new UserCredentialsTotp(userId, "JBSWY3DPEHPK3PXP");

        await _eventStore.Append(userId, totp.GetUncommittedEvents(), totp.CurrentVersion);

        var outboxRows = await GetOutboxRowsAsync(userId);
        outboxRows.Should().AllSatisfy(row =>
        {
            row.ProcessedAt.Should().BeNull();
            row.RetryCount.Should().Be(0);
            row.Error.Should().BeNull();
        });
    }

    [Fact]
    public async Task TotpInvalidated_Should_Append_Second_Event_To_EventStore_And_Outbox()
    {
        await ClearTablesAsync();
        var userId = Guid.NewGuid();
        var totp = new UserCredentialsTotp(userId, "JBSWY3DPEHPK3PXP");
        await _eventStore.Append(userId, totp.GetUncommittedEvents(), totp.CurrentVersion);

        // Rebuild and invalidate
        var loaded = new UserCredentialsTotp();
        loaded.RebuildFromEvents(await _eventStore.Load(userId));
        loaded.Invalidate();

        await _eventStore.Append(userId, loaded.GetUncommittedEvents(), loaded.CurrentVersion);

        var eventCount = await GetEventCountAsync(userId);
        var outboxRows = await GetOutboxRowsAsync(userId);

        eventCount.Should().Be(2);
        outboxRows.Should().HaveCount(2);

        outboxRows.Last().EventType.Should().Contain(nameof(TotpCredentialInvalidatedEvent));
    }

    [Fact]
    public async Task TotpEvents_Should_Rebuild_Aggregate_Correctly_From_EventStore()
    {
        await ClearTablesAsync();
        var userId = Guid.NewGuid();
        var totp = new UserCredentialsTotp(userId, "JBSWY3DPEHPK3PXP");
        await _eventStore.Append(userId, totp.GetUncommittedEvents(), totp.CurrentVersion);

        // Load and verify active state
        var loaded = new UserCredentialsTotp();
        loaded.RebuildFromEvents(await _eventStore.Load(userId));

        loaded.UserId.Should().Be(userId);
        loaded.SecurityCode.Should().Be("JBSWY3DPEHPK3PXP");
        loaded.Active.Should().BeTrue();
        loaded.CurrentVersion.Should().Be(1);

        // Invalidate, persist, reload
        loaded.Invalidate();
        await _eventStore.Append(userId, loaded.GetUncommittedEvents(), loaded.CurrentVersion);

        var reloaded = new UserCredentialsTotp();
        reloaded.RebuildFromEvents(await _eventStore.Load(userId));

        reloaded.Active.Should().BeFalse();
        reloaded.CurrentVersion.Should().Be(2);
    }

    [Fact]
    public async Task ConcurrentAppends_Same_Version_Should_Only_Commit_One()
    {
        await ClearTablesAsync();
        var userId = Guid.NewGuid();
        var totp = new UserCredentialsTotp(userId, "JBSWY3DPEHPK3PXP");
        await _eventStore.Append(userId, totp.GetUncommittedEvents(), totp.CurrentVersion);

        var writerA = new UserCredentialsTotp();
        writerA.RebuildFromEvents(await _eventStore.Load(userId));
        writerA.Invalidate();

        var writerB = new UserCredentialsTotp();
        writerB.RebuildFromEvents(await _eventStore.Load(userId));
        writerB.Invalidate();

        var taskA = _eventStore.Append(userId, writerA.GetUncommittedEvents(), writerA.CurrentVersion);
        var taskB = _eventStore.Append(userId, writerB.GetUncommittedEvents(), writerB.CurrentVersion);
        await Task.WhenAll(taskA.ContinueWith(_ => { }), taskB.ContinueWith(_ => { }));

        var tasks = new[] { taskA, taskB };
        tasks.Count(t => t.IsCompletedSuccessfully).Should().Be(1);
        tasks.Count(t => t.IsFaulted).Should().Be(1);

        var finalCount = await GetEventCountAsync(userId);
        finalCount.Should().Be(2, "initial create + one successful invalidation");
    }

    // ─── Projection dispatch test ───────────────────────────────────────────

    [Fact]
    public async Task TotpProjection_Should_Receive_Events_Via_Dispatcher()
    {
        var dispatcher = new FinancesApp_CQRS.Dispatchers.EventDispatcher();
        var handler = new FakeTotpEventHandler();
        dispatcher.Register<TotpCredentialCreatedEvent>(handler);
        dispatcher.Register<TotpCredentialInvalidatedEvent>(handler);

        var createdEvt = new TotpCredentialCreatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "JBSWY3DPEHPK3PXP");
        var invalidatedEvt = new TotpCredentialInvalidatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid());

        await dispatcher.Dispatch(createdEvt);
        await dispatcher.Dispatch(invalidatedEvt);

        handler.CreatedEvents.Should().ContainSingle().Which.Should().Be(createdEvt);
        handler.InvalidatedEvents.Should().ContainSingle().Which.Should().Be(invalidatedEvt);
    }

    // ─── Fake handler for projection dispatch test ──────────────────────────

    private sealed class FakeTotpEventHandler :
        FinancesApp_CQRS.Interfaces.IEventHandler<TotpCredentialCreatedEvent>,
        FinancesApp_CQRS.Interfaces.IEventHandler<TotpCredentialInvalidatedEvent>
    {
        public List<TotpCredentialCreatedEvent> CreatedEvents { get; } = [];
        public List<TotpCredentialInvalidatedEvent> InvalidatedEvents { get; } = [];

        public Task HandleAsync(TotpCredentialCreatedEvent evt, CancellationToken token = default)
        {
            CreatedEvents.Add(evt);
            return Task.CompletedTask;
        }

        public Task HandleAsync(TotpCredentialInvalidatedEvent evt, CancellationToken token = default)
        {
            InvalidatedEvents.Add(evt);
            return Task.CompletedTask;
        }
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
                DELETE FROM [FinanceApp].[dbo].[ProjectionCheckpoint];
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
