using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Credentials.Application.Queries.Handlers;
public class GetUserCredentialsByLoginHandler(IUserCredentialsReadRepository credentialsRepository,
                                            ILogger<GetUserCredentialsByLoginHandler> logger) : IQueryHandler<GetUserCredentialsByLogin, UserCredentials>
{
    private readonly IUserCredentialsReadRepository _credentialsRepository = credentialsRepository;
    private readonly ILogger<GetUserCredentialsByLoginHandler> _logger = logger;

    public async Task<UserCredentials> Handle(GetUserCredentialsByLogin query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching credentials - Login: {Login}",
            query.Login);

        var result = new UserCredentials();

        try
        {
            result = await _credentialsRepository.GetByLoginAsync(query.Login, token: cancellationToken);

            if (result is not null)
                _logger.LogInformation(
                    "Credentials found - Login: {Login}, UserID: {UserId}",
                    result.Login, result.UserId);
            else
                _logger.LogWarning(
                    "No credentials found - Login: {Login}",
                    query.Login);
    
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching credentials for Login {Login}", query.Login);
        }
        return result!;
    }
}
