using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_Credentials.Domain.Events;

namespace FinancesApp_Module_Credentials.Application.Projections;

public class TotpProjection(IUserCredentialsRepository credentialsRepository, IProjectionCheckpoint checkpoint) :
                             IEventHandler<TotpCredentialCreatedEvent>,
                             IEventHandler<TotpCredentialInvalidatedEvent>
{
    public async Task HandleAsync(TotpCredentialCreatedEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;

        await credentialsRepository.InvalidateUserCredentialTotpByUserIdAsync(evt.UserId, token: token);

        var totp = new UserCredentialsTotp(evt.TotpId, evt.UserId, evt.SecurityCode,
                                           evt.Timestamp, evt.Timestamp.AddMinutes(5), true);

        await credentialsRepository.SaveUserCredentialTotpAsync(totp, token: token);
    }

    public async Task HandleAsync(TotpCredentialInvalidatedEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await credentialsRepository.InvalidateUserCredentialTotpByIdAsync(evt.TotpId, token: token);
    }
}
