using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Credentials.Application.Commands.Handlers;
public class DeleteUserCredentialsHandler(IUserCredentialsRepository credentialsRepository,
                                          ILogger<DeleteUserCredentialsHandler> logger) : ICommandHandler<DeleteUserCredentials, bool>
{
    private readonly IUserCredentialsRepository _credentialsRepository = credentialsRepository;
    private readonly ILogger<DeleteUserCredentialsHandler> _logger = logger;

    public async Task<bool> Handle(DeleteUserCredentials command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Deleting credentials - UserID: {UserId}",
            command.UserId);

        try
        {
            var result = await _credentialsRepository.DeleteUserCredentialsAsync(command.UserId, token: cancellationToken);

            if (result)
                _logger.LogInformation(
                    "Credentials deleted successfully - UserID: {UserId}",
                    command.UserId);
            else
                _logger.LogWarning(
                    "Failed to delete credentials - UserID: {UserId}",
                    command.UserId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting credentials for UserID {UserId}", command.UserId);
            return false;
        }
    }
}
