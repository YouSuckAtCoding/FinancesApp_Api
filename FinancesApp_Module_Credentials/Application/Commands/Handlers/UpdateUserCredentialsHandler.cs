using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Credentials.Application.Commands.Handlers;
public class UpdateUserCredentialsHandler(IEventStore eventStore,
                                          ILogger<UpdateUserCredentialsHandler> logger) : ICommandHandler<UpdateUserCredentials, bool>
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly ILogger<UpdateUserCredentialsHandler> _logger = logger;

    private static readonly Counter CredentialsUpdated = Metrics
        .CreateCounter("credentials_total_Update", "Total number of credentials updated.");

    private static readonly Histogram CredentialsUpdateDuration = Metrics
        .CreateHistogram("credentials_Update_duration_seconds", "Credentials update processing time.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<bool> Handle(UpdateUserCredentials command, CancellationToken cancellationToken = default)
    {
        using (CredentialsUpdateDuration.NewTimer())
        {
            _logger.LogInformation(
                "Updating password - UserID: {UserId}",
                command.UserId);

            try
            {
                var events = await _eventStore.Load(command.UserId, token: cancellationToken);
                var credentials = new Domain.UserCredentials();
                credentials.RebuildFromEvents(events);

                credentials.ChangePassword(command.NewPlainPassword);

                await _eventStore.Append(command.UserId, credentials.GetUncommittedEvents(), credentials.NextVersion, cancellationToken);

                CredentialsUpdated.Inc();

                _logger.LogInformation(
                    "Password updated successfully - UserID: {UserId}",
                    command.UserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for UserID {UserId}", command.UserId);
                return false;
            }
        }
    }
}
