using System.Text.Json;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain.Events;
using FinancesApp_Module_Account.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Account.Application.Queries.Handlers;

public class GetTransactionHistoryHandler(IEventStore eventStore,
                                          IAccountReadRepository accountReadRepository,
                                          IDistributedCache cache,
                                          IConfiguration config,
                                          ILogger<GetTransactionHistoryHandler> logger)
    : IQueryHandler<GetTransactionHistory, IReadOnlyList<AccountTransaction>>
{
    private static readonly Counter Counter = Metrics
        .CreateCounter("account_total_GetTransactionHistory", "Total number of transaction history queries.");

    private static readonly Histogram Duration = Metrics
        .CreateHistogram("account_GetTransactionHistory_duration_seconds", "Transaction history query duration.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<AccountTransaction>> Handle(GetTransactionHistory query,
                                                                CancellationToken token = default)
    {
        using (Duration.NewTimer())
        {
            var key = CacheKey(query);

            try
            {
                var cached = await cache.GetStringAsync(key, token);
                if (cached is not null)
                {
                    var hit = JsonSerializer.Deserialize<List<AccountTransaction>>(cached, JsonOptions);
                    if (hit is not null)
                        return hit;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cache read failed for key {Key}", key);
            }

            try
            {
                var accounts = await accountReadRepository.GetAccounts(query.UserId, token: token);

                var transactions = new List<AccountTransaction>();

                foreach (var account in accounts)
                {
                    var events = await eventStore.Load(account.Id, token: token);

                    foreach (var evt in events)
                    {
                        var tx = MapToTransaction(evt, account.Id);

                        if (tx is null) continue;
                        if (query.From.HasValue && tx.Timestamp < query.From.Value) continue;
                        if (query.To.HasValue && tx.Timestamp > query.To.Value) continue;

                        transactions.Add(tx);
                    }
                }

                Counter.Inc();

                var result = (IReadOnlyList<AccountTransaction>)[..transactions.OrderByDescending(t => t.Timestamp)];

                try
                {
                    var ttl = config.GetValue("Redis:TransactionHistoryTtlSeconds", 30);
                    var json = JsonSerializer.Serialize(result, JsonOptions);
                    await cache.SetStringAsync(key, json,
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl) },
                        token);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Cache write failed for key {Key}", key);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving transaction history for user {UserId}", query.UserId);
                return [];
            }
        }
    }

    public static string CacheKey(GetTransactionHistory query) =>
        $"tx:{query.UserId}:{query.From?.ToString("O") ?? "null"}:{query.To?.ToString("O") ?? "null"}";

    private static AccountTransaction? MapToTransaction(IDomainEvent evt, Guid accountId) => evt switch
    {
        DepositEvent e
            => new AccountTransaction(e.EventId, accountId, e.Timestamp, TransactionKind.Deposit, e.Amount),

        WithdrawEvent e
            => new AccountTransaction(e.EventId, accountId, e.Timestamp, TransactionKind.Withdraw, e.Amount),

        CredidCardStatementPaymentEvent e
            => new AccountTransaction(e.EventId, accountId, e.Timestamp, TransactionKind.CreditCardPayment, e.Amount),

        CreditUpdatedEvent e
            => new AccountTransaction(
                e.EventId,
                accountId,
                e.Timestamp,
                TransactionKind.CreditCardChange,
                new Money(e.NewDebt.Amount - e.CurrentDebt.Amount, e.NewDebt.Currency)),

        _ => null
    };
}
