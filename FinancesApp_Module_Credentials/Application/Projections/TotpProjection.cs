using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_Credentials.Domain.Events;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Credentials.Application.Projections;

public class TotpProjection(IUserCredentialsRepository credentialsRepository,
                             IProjectionCheckpoint checkpoint,
                             ILogger<TotpProjection> logger) :
                             IEventHandler<TotpCredentialCreatedEvent>,
                             IEventHandler<TotpCredentialInvalidatedEvent>
{
    public async Task HandleAsync(TotpCredentialCreatedEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;

        logger.LogInformation("[TotpProjection] TotpCredentialCreated received. TotpId={TotpId} UserId={UserId} EventId={EventId}",
            evt.TotpId, evt.UserId, evt.EventId);

        var invalidated = await credentialsRepository.InvalidateUserCredentialTotpByUserIdAsync(evt.UserId, token: token);
        logger.LogInformation("[TotpProjection] Previous TOTPs invalidated for UserId={UserId}: {Invalidated}", evt.UserId, invalidated);

        var validUntil = evt.Timestamp.AddMinutes(2);
        var totp = new UserCredentialsTotp(evt.TotpId, evt.UserId, evt.SecurityCode,
                                           evt.Timestamp, validUntil, true);

        var saved = await credentialsRepository.SaveUserCredentialTotpAsync(totp, token: token);
        logger.LogInformation("[TotpProjection] TOTP saved. TotpId={TotpId} UserId={UserId} Active=true ValidUntil={ValidUntil} Saved={Saved}",
            evt.TotpId, evt.UserId, validUntil, saved);
    }

    public async Task HandleAsync(TotpCredentialInvalidatedEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;

        logger.LogInformation("[TotpProjection] TotpCredentialInvalidated received. TotpId={TotpId} EventId={EventId}",
            evt.TotpId, evt.EventId);

        var invalidated = await credentialsRepository.InvalidateUserCredentialTotpByIdAsync(evt.TotpId, token: token);
        logger.LogInformation("[TotpProjection] TOTP invalidated by Id. TotpId={TotpId} Success={Success}", evt.TotpId, invalidated);
    }
}
