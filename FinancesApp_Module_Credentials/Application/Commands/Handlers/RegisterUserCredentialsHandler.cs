using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Credentials.Application.Commands.Handlers;

public class RegisterUserCredentialsHandler(IUserCredentialsRepository credentialsRepository,
                                      ILogger<RegisterUserCredentialsHandler> logger) : ICommandHandler<RegisterUserCredentials, Guid>
{
    private readonly IUserCredentialsRepository _credentialsRepository = credentialsRepository;
    private readonly ILogger<RegisterUserCredentialsHandler> _logger = logger;

    public async Task<Guid> Handle(RegisterUserCredentials command, CancellationToken cancellationToken = default)
    {
        var credentials = command.Credentials;

        _logger.LogInformation(
            "Creating credentials - UserID: {UserId}, Login: {Login}",
            credentials.UserId, credentials.Login);

        try
        {
            var result = await _credentialsRepository.CreateUserCredentialsAsync(credentials, cancellationToken);

            if (result != Guid.Empty)
                _logger.LogInformation(
                    "Credentials created successfully - ID: {Id}, UserID: {UserId}, Login: {Login}",
                    result, credentials.UserId, credentials.Login);
            else
                _logger.LogWarning(
                    "Failed to create credentials - UserID: {UserId}, Login: {Login}",
                    credentials.UserId, credentials.Login);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating credentials for UserID {UserId}", credentials.UserId);
            return Guid.Empty;
        }
    }
}

