using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_User.Application.Commands.Handlers;

public class DeleteUserHandler(IEventStore eventStore,
                               ILogger<DeleteUserHandler> logger) : ICommandHandler<DeleteUser, bool>
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly ILogger<DeleteUserHandler> _logger = logger;

    private static readonly Counter UsersDeleted = Metrics
        .CreateCounter("user_total_Delete", "Total number of users deleted.");

    private static readonly Histogram UserDeleteDuration = Metrics
        .CreateHistogram("user_Delete_duration_seconds", "User deletion processing time.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<bool> Handle(DeleteUser command, CancellationToken cancellationToken = default)
    {
        using (UserDeleteDuration.NewTimer())
        {
            _logger.LogInformation("Attempting to delete user with ID: {UserId}", command.UserId);

            try
            {
                var events = await _eventStore.Load(command.UserId, token: cancellationToken);
                var user = new Domain.User();
                user.RebuildFromEvents(events);

                user.Delete();

                await _eventStore.Append(command.UserId, user.GetUncommittedEvents(), user.NextVersion, cancellationToken);

                UsersDeleted.Inc();

                _logger.LogInformation("User deleted successfully - ID: {UserId}", command.UserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", command.UserId);
                return false;
            }
        }
    }
}
