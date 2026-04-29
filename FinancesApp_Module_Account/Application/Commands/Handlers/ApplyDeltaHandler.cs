using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain.Events;
using Microsoft.Extensions.Logging;

using Prometheus;


namespace FinancesApp_Module_Account.Application.Commands.Handlers;
public class ApplyDeltaHandler : ICommandHandler<ApplyDelta, ApplyDeltaResult>
{
    public static readonly TimeSpan RequestedAtSkewTolerance = TimeSpan.FromMinutes(5);

    private readonly ILogger<CreateAccountHandler> _logger;
    private readonly IEventStore _eventStore;

    private static readonly Counter TransactionsCreated = Metrics
        .CreateCounter("account_total_ApplyDelta", "Total number of transactions / ApplyDelta");

    private static readonly Counter TransactionFailures = Metrics
        .CreateCounter("account_total_ApplyDelta_failures", "Total number of failed ApplyDelta attempts");

    private static readonly Histogram TransactionProcessingDuration = Metrics
        .CreateHistogram("account_transaction_duration_seconds", "Transaction processing time",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public ApplyDeltaHandler(ILogger<CreateAccountHandler> logger, IAccountRepository accountRepository, IEventStore eventStore)
    {
        _logger = logger;
        _eventStore = eventStore;
    }

    public async Task<ApplyDeltaResult> Handle(ApplyDelta command, CancellationToken cancellationToken = default)
    {
        var account = command.Account;

        using (TransactionProcessingDuration.NewTimer())
        {
            _logger.LogInformation("Transction happening at {RequestedAt} for - ID: {AccountId}, UserId: {UserId}, Type: {Type}, Balance: {Balance}, Status: {Status}",
                                    command.RequestedAt, account.Id, account.UserId, account.Type, account.Balance, account.Status);

            var skewError = ValidateRequestedAt(command.RequestedAt);
            if (skewError is not null)
            {
                TransactionFailures.Inc();
                return ApplyDeltaResult.Failed(skewError);
            }

            await _eventStore.Load(account.Id, token: cancellationToken);

            account.ApplyDelta(command.Delta, command.OperationType);

            var uncommitted = account.GetUncommittedEvents();
            var error = uncommitted.OfType<ApplyDeltaErrorEvent>().FirstOrDefault();

            await _eventStore.Append(account.Id, uncommitted, account.CurrentVersion, cancellationToken);

            if (error is not null)
            {
                _logger.LogWarning(
                    "ApplyDelta rejected for AccountId={AccountId}: {Error}. Attempted {Operation} {Delta}",
                    account.Id, error.ErrorMessage, error.AttemptedOperationType, error.AttemptedDelta);
                TransactionFailures.Inc();
                return ApplyDeltaResult.Failed(error.ErrorMessage);
            }

            TransactionsCreated.Inc();
            return ApplyDeltaResult.Ok();
        }
    }

    private static string? ValidateRequestedAt(DateTimeOffset requestedAt)
    {
        var skew = (requestedAt - DateTimeOffset.UtcNow).Duration();
        return skew > RequestedAtSkewTolerance
            ? "RequestedAt is outside the allowed time window."
            : null;
    }
}
