using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Credentials.Application.Queries.Handlers;
public class GetUserCredentialsByUserIdHandler(IUserCredentialsReadRepository credentialsRepository,
                                           ILogger<GetUserCredentialsByUserIdHandler> logger) : IQueryHandler<GetUserCredentialsByUserId, UserCredentials>
{
    private readonly IUserCredentialsReadRepository _credentialsRepository = credentialsRepository;
    private readonly ILogger<GetUserCredentialsByUserIdHandler> _logger = logger;

    public async Task<UserCredentials> Handle(GetUserCredentialsByUserId query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching credentials - UserID: {UserId}",
            query.UserId);

        var result = new UserCredentials();

        try
        {
            result = await _credentialsRepository.GetByUserIdAsync(query.UserId, cancellationToken);

            if (result is not null)
                _logger.LogInformation(
                    "Credentials found - UserID: {UserId}, Login: {Login}",
                    result.UserId, result.Login);
            else
                _logger.LogWarning(
                    "No credentials found - UserID: {UserId}",
                    query.UserId);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching credentials for UserID {UserId}", query.UserId);
        }
        return result!;
    }
}
