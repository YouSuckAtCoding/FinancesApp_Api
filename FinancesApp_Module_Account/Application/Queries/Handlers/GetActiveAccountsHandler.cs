using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Account.Application.Queries.Handlers;
public class GetActiveAccountsHandler(IAccountReadRepository accountRepository,
                                        ILogger<GetActiveAccountsHandler> logger) : IQueryHandler<GetActiveAccounts, IReadOnlyList<Account>>
{
    private readonly IAccountReadRepository _accountRepository = accountRepository;
    private readonly ILogger<GetActiveAccountsHandler> _logger = logger;

    private static readonly Counter GetActiveAccountsCounter = Metrics
        .CreateCounter("account_total_GetActive", "Total number of get active accounts queries.");

    private static readonly Histogram GetActiveAccountsDuration = Metrics
        .CreateHistogram("account_GetActive_duration_seconds", "Get active accounts query duration.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<IReadOnlyList<Account>> Handle(GetActiveAccounts query, CancellationToken cancellationToken = default)
    {
        using (GetActiveAccountsDuration.NewTimer())
        {
            try
            {
                var result = await _accountRepository.GetActiveAccounts(token: cancellationToken);

                GetActiveAccountsCounter.Inc();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active accounts");
                return [];
            }
        }
    }
}
