using FinancesApp_CQRS.Interfaces;
using FinancesApp_CQRS.Queries;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Account.Application.Queries.Handlers;
public class GetAccountsHandler(IAccountReadRepository accountRepository,
                                ILogger<GetAccountsHandler> logger) : IQueryHandler<GetAccounts, IReadOnlyList<Account>>
{

    private readonly IAccountReadRepository _accountRepository = accountRepository;
    private readonly ILogger<GetAccountsHandler> _logger = logger;

    private static readonly Counter GetAccountsCounter = Metrics
        .CreateCounter("account_total_GetAll", "Total number of get all accounts queries.");

    private static readonly Histogram GetAccountsDuration = Metrics
        .CreateHistogram("account_GetAll_duration_seconds", "Get all accounts query duration.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<IReadOnlyList<Account>> Handle(GetAccounts query, CancellationToken cancellationToken = default)
    {
        using (GetAccountsDuration.NewTimer())
        {
            try
            {
                var result = await _accountRepository.GetAccounts(token: cancellationToken);

                GetAccountsCounter.Inc();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accounts");
                return [];
            }
        }
    }
}
