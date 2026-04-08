using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Credentials.Application.Commands.Handlers;
public class DeleteUserCredentialsHandler(IEventStore eventStore,
                                          ILogger<DeleteUserCredentialsHandler> logger) : ICommandHandler<DeleteUserCredentials, bool>
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly ILogger<DeleteUserCredentialsHandler> _logger = logger;

    private static readonly Counter CredentialsDeleted = Metrics
        .CreateCounter("credentials_total_Delete", "Total number of credentials deleted.");

    private static readonly Histogram CredentialsDeleteDuration = Metrics
        .CreateHistogram("credentials_Delete_duration_seconds", "Credentials deletion processing time.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<bool> Handle(DeleteUserCredentials command, CancellationToken cancellationToken = default)
    {
        using (CredentialsDeleteDuration.NewTimer())
        {
            _logger.LogInformation("Attempting to delete credentials for UserID: {UserId}", command.UserId);

            try
            {
                var events = await _eventStore.Load(command.UserId, token: cancellationToken);
                var credentials = new Domain.UserCredentials();
                credentials.RebuildFromEvents(events);

                credentials.Delete();

                await _eventStore.Append(command.UserId, credentials.GetUncommittedEvents(), credentials.NextVersion, cancellationToken);

                CredentialsDeleted.Inc();

                _logger.LogInformation("Credentials deleted successfully - UserID: {UserId}", command.UserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting credentials for UserID {UserId}", command.UserId);
                return false;
            }
        }
    }
}
