using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_User.Application.Commands.Handlers;

public class UpdateUserHandler(IEventStore eventStore,
                               ILogger<UpdateUserHandler> logger) : ICommandHandler<UpdateUser, bool>
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly ILogger<UpdateUserHandler> _logger = logger;

    private static readonly Counter UsersUpdated = Metrics
        .CreateCounter("user_total_Update", "Total number of users updated.");

    private static readonly Histogram UserUpdateDuration = Metrics
        .CreateHistogram("user_Update_duration_seconds", "User update processing time.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<bool> Handle(UpdateUser command, CancellationToken cancellationToken = default)
    {
        using (UserUpdateDuration.NewTimer())
        {
            var user = command.User;

            _logger.LogInformation(
                "Updating user - ID: {UserId}, Name: {Name}, Email: {Email}, ModifiedAt: {ModifiedAt}",
                user.Id, user.Name, user.Email, user.ModifiedAt);

            try
            {
                var events = await _eventStore.Load(user.Id, token: cancellationToken);
                var existing = new Domain.User();
                existing.RebuildFromEvents(events);

                await _eventStore.Append(user.Id, user.GetUncommittedEvents(), existing.NextVersion, cancellationToken);

                UsersUpdated.Inc();

                _logger.LogInformation(
                    "User updated successfully - ID: {UserId}, Name: {Name}, Email: {Email}, ModifiedAt: {ModifiedAt}, DateOfBirth: {DateOfBirth}, ProfileImage: {ProfileImage}",
                    user.Id, user.Name, user.Email, user.ModifiedAt, user.DateOfBirth, user.ProfileImage);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", user.Id);
                return false;
            }
        }
    }
}
