using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_Credentials.Domain.Events;

namespace FinancesApp_Module_Credentials.Application.Projections;

public class CredentialsProjection(IUserCredentialsRepository credentialsRepository, IProjectionCheckpoint checkpoint) :
                                   IEventHandler<CredentialsRegisteredEvent>,
                                   IEventHandler<CredentialsPasswordChangedEvent>,
                                   IEventHandler<CredentialsDeletedEvent>
{
    public async Task HandleAsync(CredentialsRegisteredEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await credentialsRepository.CreateUserCredentialsAsync(
            new UserCredentials(evt.Id, evt.UserId, evt.Email, evt.PasswordHash), token: token);
    }

    public async Task HandleAsync(CredentialsPasswordChangedEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await credentialsRepository.UpdatePasswordAsync(evt.UserId, evt.NewPasswordHash, token: token);
    }

    public async Task HandleAsync(CredentialsDeletedEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await credentialsRepository.DeleteUserCredentialsAsync(evt.UserId, token: token);
    }
}
