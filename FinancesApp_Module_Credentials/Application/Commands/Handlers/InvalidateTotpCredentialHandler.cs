using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Credentials.Application.Commands.Handlers;
public class InvalidateTotpCredentialHandler(IEventStore eventStore,
                                             ILogger<InvalidateTotpCredentialHandler> logger) : ICommandHandler<InvalidateTotpCredential, bool>
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly ILogger<InvalidateTotpCredentialHandler> _logger = logger;

    private static readonly Counter TotpInvalidated = Metrics
        .CreateCounter("totp_credentials_invalidated_total", "Total number of TOTP credentials invalidated.");

    public async Task<bool> Handle(InvalidateTotpCredential command, CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _eventStore.Load(command.TotpId, token: cancellationToken);

            if (events.Count == 0)
                return false;

            var totp = new UserCredentialsTotp();
            totp.RebuildFromEvents(events);

            if (!totp.Active)
                return false;

            totp.Invalidate();

            await _eventStore.Append(command.TotpId, totp.GetUncommittedEvents(), totp.CurrentVersion, cancellationToken);

            TotpInvalidated.Inc();

            _logger.LogInformation("TOTP invalidated for TotpID {TotpId} (UserID {UserId})", command.TotpId, totp.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating TOTP for TotpID {TotpId}", command.TotpId);
            return false;
        }
    }
}
