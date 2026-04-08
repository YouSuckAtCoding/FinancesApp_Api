using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Credentials.Application.Commands.Handlers;

public class RegisterUserCredentialsHandler(IEventStore eventStore,
                                      ILogger<RegisterUserCredentialsHandler> logger) : ICommandHandler<RegisterUserCredentials, Guid>
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly ILogger<RegisterUserCredentialsHandler> _logger = logger;

    private static readonly Counter CredentialsCreated = Metrics
        .CreateCounter("credentials_total_Create", "Total number of credentials created.");

    private static readonly Histogram CredentialsCreationDuration = Metrics
        .CreateHistogram("credentials_Create_duration_seconds", "Credentials creation processing time.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<Guid> Handle(RegisterUserCredentials command, CancellationToken cancellationToken = default)
    {
        using (CredentialsCreationDuration.NewTimer())
        {
            var credentials = command.Credentials;

            _logger.LogInformation(
                "Creating credentials - UserID: {UserId}, Login: {Login}",
                credentials.UserId, credentials.Email);

            try
            {
                await _eventStore.Append(credentials.UserId, credentials.GetUncommittedEvents(), credentials.CurrentVersion, cancellationToken);

                CredentialsCreated.Inc();

                _logger.LogInformation(
                    "Credentials created successfully - ID: {Id}, UserID: {UserId}, Login: {Login}",
                    credentials.Id, credentials.UserId, credentials.Email);

                return credentials.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating credentials for UserID {UserId}", credentials.UserId);
                return Guid.Empty;
            }
        }
    }
}
