using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Credentials.Application.Commands.Handlers;
public class UpdateUserCredentialsHandler(IUserCredentialsRepository credentialsRepository,
                                          ILogger<UpdateUserCredentialsHandler> logger) : ICommandHandler<UpdateUserCredentials, bool>
{
    private readonly IUserCredentialsRepository _credentialsRepository = credentialsRepository;
    private readonly ILogger<UpdateUserCredentialsHandler> _logger = logger;

    public async Task<bool> Handle(UpdateUserCredentials command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Updating password - UserID: {UserId}",
            command.UserId);

        try
        {
            var credentials = new UserCredentials();
            credentials.SetPassword(command.NewPlainPassword);

            var result = await _credentialsRepository.UpdatePasswordAsync(command.UserId, credentials.Password, token: cancellationToken);

            if (result)
                _logger.LogInformation(
                    "Password updated successfully - UserID: {UserId}",
                    command.UserId);
            else
                _logger.LogWarning(
                    "Failed to update password - UserID: {UserId}",
                    command.UserId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password for UserID {UserId}", command.UserId);
            return false;
        }
    }
}

