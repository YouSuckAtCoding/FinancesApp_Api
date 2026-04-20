using FinanceAppDatabase.DbConnection;
using FinancesApp_CQRS.Dispatchers;
using FinancesApp_CQRS.EventStore;
using FinancesApp_CQRS.Projections;
using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Commands.Handlers;
using FinancesApp_Module_Credentials.Application.Projections;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_Credentials.Domain.Events;
using FinancesApp_Tests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancesApp_Tests.UserCredentialsTests;

public class RebuildCredentialsProjectionIntegrationTests : IClassFixture<SqlFixture>
{
    private readonly EventStore _eventStore;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICommandFactory _commandFactory;

    public RebuildCredentialsProjectionIntegrationTests(SqlFixture fixture)
    {
        _connectionFactory = fixture.ConnectionFactory;
        _commandFactory = fixture.CommandFactory;
        _eventStore = new EventStore(_commandFactory, _connectionFactory);
    }

    [Fact]
    public async Task Rebuild_Should_Restore_Credentials_After_Projection_Row_Deleted()
    {
        // Each test uses a unique userId — no need to clear shared tables
        var userId = await CreateTestUserAsync();

        // 2. Create credentials via event sourcing
        var credentials = new UserCredentials(userId, "rebuild_test@email.com", "SecurePass1!");
        await _eventStore.Append(userId, credentials.GetUncommittedEvents(), credentials.CurrentVersion);

        // 3. Set up projection and dispatcher, then project the events
        var dispatcher = BuildDispatcherWithProjections();
        var events = await _eventStore.Load(userId);
        foreach (var evt in events)
            await dispatcher.Dispatch(evt);

        // 4. Verify projection row exists
        var beforeDelete = await GetCredentialsReadModelAsync(userId);
        beforeDelete.Should().NotBeNull();
        beforeDelete!.Email.Should().Be("rebuild_test@email.com");

        // 5. Simulate the fuckup: delete the projection row directly
        await DeleteCredentialsReadModelAsync(userId);
        var afterDelete = await GetCredentialsReadModelAsync(userId);
        afterDelete.Should().BeNull("projection row was deleted");

        // 6. Rebuild projection
        var handler = new RebuildCredentialsProjectionHandler(
            _eventStore, dispatcher, _connectionFactory,
            NullLogger<RebuildCredentialsProjectionHandler>.Instance);

        var result = await handler.Handle(new RebuildCredentialsProjection(userId));

        // 7. Verify projection row is restored
        result.Should().BeTrue();
        var restored = await GetCredentialsReadModelAsync(userId);
        restored.Should().NotBeNull();
        restored!.Email.Should().Be("rebuild_test@email.com");
        restored.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Rebuild_Should_Restore_Totp_After_Projection_Row_Deleted()
    {
        var userId = await CreateTestUserAsync();

        // 1. Create TOTP via event sourcing
        var totp = new UserCredentialsTotp(userId, "JBSWY3DPEHPK3PXP");
        await _eventStore.Append(userId, totp.GetUncommittedEvents(), totp.CurrentVersion);

        // 2. Project the events
        var dispatcher = BuildDispatcherWithProjections();
        var events = await _eventStore.Load(userId);
        foreach (var evt in events)
            await dispatcher.Dispatch(evt);

        // 3. Verify TOTP projection row exists
        var beforeDelete = await GetTotpReadModelAsync(userId);
        beforeDelete.Should().NotBeNull();
        beforeDelete!.SecurityCode.Should().Be("JBSWY3DPEHPK3PXP");

        // 4. Delete TOTP projection row (simulate the fuckup)
        await DeleteTotpReadModelAsync(userId);
        var afterDelete = await GetTotpReadModelAsync(userId);
        afterDelete.Should().BeNull();

        // 5. Rebuild
        var handler = new RebuildCredentialsProjectionHandler(
            _eventStore, dispatcher, _connectionFactory,
            NullLogger<RebuildCredentialsProjectionHandler>.Instance);

        var result = await handler.Handle(new RebuildCredentialsProjection(userId));

        // 6. Verify restored
        result.Should().BeTrue();
        var restored = await GetTotpReadModelAsync(userId);
        restored.Should().NotBeNull();
        restored!.SecurityCode.Should().Be("JBSWY3DPEHPK3PXP");
        restored.Active.Should().BeTrue();
    }

    [Fact]
    public async Task Rebuild_Should_Reflect_Latest_State_After_Multiple_Events()
    {
        var userId = await CreateTestUserAsync();

        // Create credentials then change password
        var credentials = new UserCredentials(userId, "multi_event@email.com", "SecurePass1!");
        await _eventStore.Append(userId, credentials.GetUncommittedEvents(), credentials.CurrentVersion);

        var loaded = new UserCredentials();
        loaded.RebuildFromEvents(await _eventStore.Load(userId));
        loaded.ChangePassword("NewSecurePass2!");
        await _eventStore.Append(userId, loaded.GetUncommittedEvents(), loaded.CurrentVersion);

        // Project
        var dispatcher = BuildDispatcherWithProjections();
        foreach (var evt in await _eventStore.Load(userId))
            await dispatcher.Dispatch(evt);

        // Delete projection row
        await DeleteCredentialsReadModelAsync(userId);

        // Rebuild
        var handler = new RebuildCredentialsProjectionHandler(
            _eventStore, dispatcher, _connectionFactory,
            NullLogger<RebuildCredentialsProjectionHandler>.Instance);

        var result = await handler.Handle(new RebuildCredentialsProjection(userId));

        result.Should().BeTrue();
        var restored = await GetCredentialsReadModelAsync(userId);
        restored.Should().NotBeNull();

        // Password should be the LATEST hash (from ChangePassword), not the original
        BCrypt.Net.BCrypt.Verify("NewSecurePass2!", restored!.Password).Should().BeTrue();
    }

    [Fact]
    public async Task Rebuild_Should_Return_False_When_No_Events_Exist()
    {
        var dispatcher = BuildDispatcherWithProjections();
        var handler = new RebuildCredentialsProjectionHandler(
            _eventStore, dispatcher, _connectionFactory,
            NullLogger<RebuildCredentialsProjectionHandler>.Instance);

        var result = await handler.Handle(new RebuildCredentialsProjection(Guid.NewGuid()));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Rebuild_Should_Clear_Old_Checkpoints_So_Projection_Can_Reclaim()
    {
        var userId = await CreateTestUserAsync();

        var credentials = new UserCredentials(userId, "checkpoint@email.com", "SecurePass1!");
        await _eventStore.Append(userId, credentials.GetUncommittedEvents(), credentials.CurrentVersion);

        // Project events (this claims checkpoints)
        var dispatcher = BuildDispatcherWithProjections();
        foreach (var evt in await _eventStore.Load(userId))
            await dispatcher.Dispatch(evt);

        // Verify checkpoints exist
        var checkpointCount = await GetCheckpointCountForAggregateAsync(userId);
        checkpointCount.Should().BeGreaterThan(0);

        // Delete read model
        await DeleteCredentialsReadModelAsync(userId);

        // Rebuild — this must clear checkpoints first, otherwise projection won't reclaim
        var handler = new RebuildCredentialsProjectionHandler(
            _eventStore, dispatcher, _connectionFactory,
            NullLogger<RebuildCredentialsProjectionHandler>.Instance);

        var result = await handler.Handle(new RebuildCredentialsProjection(userId));

        result.Should().BeTrue();

        // Checkpoints should exist again (re-claimed by projection)
        var newCheckpointCount = await GetCheckpointCountForAggregateAsync(userId);
        newCheckpointCount.Should().Be(checkpointCount);

        // Read model should be restored
        var restored = await GetCredentialsReadModelAsync(userId);
        restored.Should().NotBeNull();
    }

    [Fact]
    public async Task Rebuild_Should_Be_Idempotent()
    {
        var userId = await CreateTestUserAsync();

        var credentials = new UserCredentials(userId, "idempotent@email.com", "SecurePass1!");
        await _eventStore.Append(userId, credentials.GetUncommittedEvents(), credentials.CurrentVersion);

        var dispatcher = BuildDispatcherWithProjections();
        foreach (var evt in await _eventStore.Load(userId))
            await dispatcher.Dispatch(evt);

        // Rebuild twice — second run should not fail or duplicate data
        var handler = new RebuildCredentialsProjectionHandler(
            _eventStore, dispatcher, _connectionFactory,
            NullLogger<RebuildCredentialsProjectionHandler>.Instance);

        var result1 = await handler.Handle(new RebuildCredentialsProjection(userId));
        var result2 = await handler.Handle(new RebuildCredentialsProjection(userId));

        result1.Should().BeTrue();
        result2.Should().BeTrue();

        var credCount = await GetCredentialsRowCountAsync(userId);
        credCount.Should().Be(1, "rebuild should not duplicate rows");
    }

    // ─── Builder ────────────────────────────────────────────────────────────

    private EventDispatcher BuildDispatcherWithProjections()
    {
        var dispatcher = new EventDispatcher();
        var checkpoint = new ProjectionCheckpoint(_commandFactory);
        var credentialsRepo = new UserCredentialsRepository(_connectionFactory, _commandFactory);

        var credentialsProjection = new CredentialsProjection(credentialsRepo, checkpoint);
        dispatcher.Register<CredentialsRegisteredEvent>(credentialsProjection);
        dispatcher.Register<CredentialsPasswordChangedEvent>(credentialsProjection);
        dispatcher.Register<CredentialsDeletedEvent>(credentialsProjection);

        var totpProjection = new TotpProjection(credentialsRepo, checkpoint);
        dispatcher.Register<TotpCredentialCreatedEvent>(totpProjection);
        dispatcher.Register<TotpCredentialInvalidatedEvent>(totpProjection);

        return dispatcher;
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task<Guid> CreateTestUserAsync()
    {
        var userId = Guid.NewGuid();
        await _connectionFactory.ExecuteInScopeAsync(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO [FinanceApp].[dbo].[Users] (Id, Name, Email, RegisteredAt, ModifiedAt, DateOfBirth)
                VALUES (@Id, @Name, @Email, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), '1990-01-01')
                """;
            cmd.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = userId });
            cmd.Parameters.Add(new SqlParameter("@Name", $"TestUser_{userId:N}"));
            cmd.Parameters.Add(new SqlParameter("@Email", $"{userId:N}@test.com"));
            await cmd.ExecuteNonQueryAsync();
        });
        return userId;
    }

    private async Task<CredentialsRow?> GetCredentialsReadModelAsync(Guid userId)
    {
        return await _connectionFactory.ExecuteInScopeAsync<CredentialsRow?>(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, UserId, Login, PasswordHash FROM [FinanceApp].[dbo].[UserCredentials] WHERE UserId = @UserId";
            cmd.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.UniqueIdentifier) { Value = userId });
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return new CredentialsRow(
                reader.GetGuid(0), reader.GetGuid(1),
                reader.GetString(2), reader.GetString(3));
        });
    }

    private async Task<TotpRow?> GetTotpReadModelAsync(Guid userId)
    {
        return await _connectionFactory.ExecuteInScopeAsync<TotpRow?>(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT UserId, SecurityCode, Active FROM [FinanceApp].[dbo].[UserCredentialsTotp] WHERE UserId = @UserId AND Active = 1";
            cmd.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.UniqueIdentifier) { Value = userId });
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return new TotpRow(reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2));
        });
    }

    private async Task DeleteCredentialsReadModelAsync(Guid userId)
    {
        await _connectionFactory.ExecuteInScopeAsync(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM [FinanceApp].[dbo].[UserCredentials] WHERE UserId = @UserId";
            cmd.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.UniqueIdentifier) { Value = userId });
            await cmd.ExecuteNonQueryAsync();
        });
    }

    private async Task DeleteTotpReadModelAsync(Guid userId)
    {
        await _connectionFactory.ExecuteInScopeAsync(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM [FinanceApp].[dbo].[UserCredentialsTotp] WHERE UserId = @UserId";
            cmd.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.UniqueIdentifier) { Value = userId });
            await cmd.ExecuteNonQueryAsync();
        });
    }

    private async Task<int> GetCheckpointCountForAggregateAsync(Guid userId)
    {
        var events = await _eventStore.Load(userId);
        var count = 0;
        foreach (var evt in events)
        {
            count += await _connectionFactory.ExecuteInScopeAsync<int>(async conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM [FinanceApp].[dbo].[ProjectionCheckpoint] WHERE event_id = @eventId";
                cmd.Parameters.Add(new SqlParameter("@eventId", System.Data.SqlDbType.UniqueIdentifier) { Value = evt.EventId });
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            });
        }
        return count;
    }

    private async Task<int> GetCredentialsRowCountAsync(Guid userId)
    {
        return await _connectionFactory.ExecuteInScopeAsync<int>(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM [FinanceApp].[dbo].[UserCredentials] WHERE UserId = @UserId";
            cmd.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.UniqueIdentifier) { Value = userId });
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        });
    }

    private record CredentialsRow(Guid Id, Guid UserId, string Email, string Password);
    private record TotpRow(Guid UserId, string SecurityCode, bool Active);
}
