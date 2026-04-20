using FinancesApp_CQRS.EventStore;
using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Credentials.Application.Commands.Handlers;
public class TotpCredentialCreatedHandler(IEventStore eventStore,
                                          ILogger<TotpCredentialCreatedHandler> logger) : ICommandHandler<TotpCredentialCreated, bool>
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly ILogger<TotpCredentialCreatedHandler> _logger = logger;

    private static readonly Counter TotpCredentialsCreated = Metrics
      .CreateCounter("totp_credentials_created_total", "Total number of TOTP credentials created.");

    private static readonly Histogram TotpCredentialsCreatedDuration = Metrics
        .CreateHistogram("totp_credentials_created_duration_seconds", "TOTP credentials creation processing time.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<bool> Handle(TotpCredentialCreated command, CancellationToken cancellationToken = default)
    {
        using (TotpCredentialsCreatedDuration.NewTimer())
        {
            _logger.LogInformation(
            "Creating TOTP credentials - UserID: {UserId}, CreatedAt: {CreatedAt}",
            command.TotpCredentials.UserId, command.TotpCredentials.CreatedAt);

            try
            {
                await _eventStore.Append(command.TotpCredentials.Id, command.TotpCredentials.GetUncommittedEvents(), command.TotpCredentials.CurrentVersion, cancellationToken);

                TotpCredentialsCreated.Inc();

                _logger.LogInformation(
                    "TOTP credentials created successfully - UserID: {UserId}, CreatedAt: {CreatedAt}",
                    command.TotpCredentials.UserId, command.TotpCredentials.CreatedAt);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating TOTP credentials for UserID {UserId}", command.TotpCredentials.UserId);
                return false;
            }
        }
    }
}
