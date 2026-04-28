using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Credentials.Application.Queries.Handlers;
public class GetUserCredentialsByUserIdHandler(IUserCredentialsReadRepository readRepository,
                                               ILogger<GetUserCredentialsByUserIdHandler> logger) : IQueryHandler<GetUserCredentialsByUserId, UserCredentials>
{
    private static readonly Counter GetCredentialsByUserIdCounter = Metrics
        .CreateCounter("credentials_total_GetByUserId", "Total number of credentials retrieved by user id.");

    private static readonly Histogram GetCredentialsByUserIdDuration = Metrics
        .CreateHistogram("credentials_GetByUserId_duration_seconds", "Credentials retrieved by user id duration.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<UserCredentials> Handle(GetUserCredentialsByUserId query, CancellationToken cancellationToken = default)
    {
        using (GetCredentialsByUserIdDuration.NewTimer())
        {
            try
            {
                var credentials = await readRepository.GetByUserIdAsync(query.UserId, token: cancellationToken);

                GetCredentialsByUserIdCounter.Inc();

                return credentials;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching credentials for UserID {UserId}", query.UserId);
                return new UserCredentials();
            }
        }
    }
}
