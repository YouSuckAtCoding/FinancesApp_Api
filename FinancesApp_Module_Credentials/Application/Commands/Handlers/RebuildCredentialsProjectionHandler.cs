using FinanceAppDatabase.DbConnection;
using FinancesApp_CQRS.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Credentials.Application.Commands.Handlers;

public class RebuildCredentialsProjectionHandler(IEventStore eventStore,
                                                 IEventDispatcher eventDispatcher,
                                                 IDbConnectionFactory connectionFactory,
                                                 ILogger<RebuildCredentialsProjectionHandler> logger) : ICommandHandler<RebuildCredentialsProjection, bool>
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly IEventDispatcher _eventDispatcher = eventDispatcher;
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;
    private readonly ILogger<RebuildCredentialsProjectionHandler> _logger = logger;

    public async Task<bool> Handle(RebuildCredentialsProjection command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rebuilding credentials projection for UserID {UserId}", command.UserId);

        try
        {
            // 1. Load all events for this aggregate from the event store
            var events = await _eventStore.Load(command.UserId, token: cancellationToken);

            if (events.Count == 0)
            {
                _logger.LogWarning("No events found for UserID {UserId}, nothing to rebuild", command.UserId);
                return false;
            }

            // 2. Clear projection checkpoints for these events so they can be re-claimed
            // 3. Clear projection read model rows for this user
            await _connectionFactory.ExecuteInScopeWithTransactionAsync(async (conn, tx) =>
            {
                // Delete checkpoint entries for all event IDs belonging to this aggregate
                foreach (var evt in events)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = "DELETE FROM [FinanceApp].[dbo].[ProjectionCheckpoint] WHERE event_id = @eventId";
                    cmd.Parameters.Add(new SqlParameter("@eventId", System.Data.SqlDbType.UniqueIdentifier) { Value = evt.EventId });
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                // Delete credentials read model row for this user
                using var deleteCredentials = conn.CreateCommand();
                deleteCredentials.Transaction = tx;
                deleteCredentials.CommandText = "DELETE FROM [FinanceApp].[dbo].[UserCredentials] WHERE UserId = @UserId";
                deleteCredentials.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.UniqueIdentifier) { Value = command.UserId });
                await deleteCredentials.ExecuteNonQueryAsync(cancellationToken);

                // Delete TOTP read model rows for this user
                using var deleteTotp = conn.CreateCommand();
                deleteTotp.Transaction = tx;
                deleteTotp.CommandText = "DELETE FROM [FinanceApp].[dbo].[UserCredentialsTotp] WHERE UserId = @UserId";
                deleteTotp.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.UniqueIdentifier) { Value = command.UserId });
                await deleteTotp.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);

            // 4. Re-dispatch all events through the projection handlers
            // The projections will re-claim via ProjectionCheckpoint and rebuild the read model
            foreach (var evt in events)
            {
                await _eventDispatcher.Dispatch(evt, cancellationToken);
            }

            _logger.LogInformation(
                "Credentials projection rebuilt for UserID {UserId} — {EventCount} events replayed",
                command.UserId, events.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding credentials projection for UserID {UserId}", command.UserId);
            return false;
        }
    }
}
