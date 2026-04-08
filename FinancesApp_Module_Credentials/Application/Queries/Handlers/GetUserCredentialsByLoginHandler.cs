using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Credentials.Application.Queries.Handlers;
public class GetUserCredentialsByLoginHandler(IUserCredentialsReadRepository credentialsRepository,
                                            ILogger<GetUserCredentialsByLoginHandler> logger) : IQueryHandler<GetUserCredentialsByLogin, UserCredentials>
{
    private readonly IUserCredentialsReadRepository _credentialsRepository = credentialsRepository;
    private readonly ILogger<GetUserCredentialsByLoginHandler> _logger = logger;

    private static readonly Counter GetCredentialsByLoginCounter = Metrics
        .CreateCounter("credentials_total_GetByLogin", "Total number of credentials retrieved by login.");

    private static readonly Histogram GetCredentialsByLoginDuration = Metrics
        .CreateHistogram("credentials_GetByLogin_duration_seconds", "Credentials retrieved by login duration.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<UserCredentials> Handle(GetUserCredentialsByLogin query, CancellationToken cancellationToken = default)
    {
        using (GetCredentialsByLoginDuration.NewTimer())
        {
            try
            {
                var result = await _credentialsRepository.GetByLoginAsync(query.Login, token: cancellationToken);

                GetCredentialsByLoginCounter.Inc();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching credentials for Login {Login}", query.Login);
                return new UserCredentials();
            }
        }
    }
}
