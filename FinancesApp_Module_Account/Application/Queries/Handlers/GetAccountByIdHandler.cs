using FinancesApp_CQRS.EventStore;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Queries;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;
using System.Threading;

namespace FinancesApp_Module_Account.Application.Queries.Handlers;
public class GetAccountByIdHandler(ILogger<GetAccountByIdHandler> logger,
                                   IEventStore eventStore) : IQueryHandler<GetAccountById, Account>
{
    private readonly ILogger<GetAccountByIdHandler> _logger = logger;
    private readonly IEventStore _eventStore = eventStore;


    private static readonly Counter GetAccountByIdCounter = Metrics
    .CreateCounter("account_total_GetById", "Total number of accounts retrieved by id.");

    private static readonly Histogram GetAccountByIdDuration = Metrics
        .CreateHistogram("account_GetById_duration_seconds", "Accounts retrieved by id.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<Account> Handle(GetAccountById query,
                                      CancellationToken token = default)
    {
        using(GetAccountByIdDuration.NewTimer())
        {
            var account = new Account();
            try
            {
                var loaded = await _eventStore.Load(query.AccountId, token: token);

                account.RebuildFromEvents(loaded);

                GetAccountByIdCounter.Inc();

                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account with ID {AccountId}", query.AccountId);
                return new Account();
            }
        }
        
    }
}
