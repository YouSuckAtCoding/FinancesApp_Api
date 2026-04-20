using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Credentials.Application.Queries.Handlers;
public class GetActiveUserTotpHandler(IUserCredentialsTotpReadRepository readRepository,
                                      ILogger<GetActiveUserTotpHandler> logger) : IQueryHandler<GetActiveUserTotp, UserCredentialsTotp?>
{
    private readonly IUserCredentialsTotpReadRepository _readRepository = readRepository;
    private readonly ILogger<GetActiveUserTotpHandler> _logger = logger;

    private static readonly Counter GetActiveTotpCounter = Metrics
        .CreateCounter("totp_total_GetActiveByUserId", "Total number of active TOTP credentials retrieved.");

    private static readonly Histogram GetActiveTotpDuration = Metrics
        .CreateHistogram("totp_GetActiveByUserId_duration_seconds", "Active TOTP retrieval duration.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<UserCredentialsTotp?> Handle(GetActiveUserTotp query, CancellationToken cancellationToken = default)
    {
        using (GetActiveTotpDuration.NewTimer())
        {
            try
            {
                var totp = await _readRepository.GetActiveTotpByUserIdAsync(query.UserId, token: cancellationToken);

                GetActiveTotpCounter.Inc();

                return totp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active TOTP for UserID {UserId}", query.UserId);
                return null;
            }
        }
    }
}
