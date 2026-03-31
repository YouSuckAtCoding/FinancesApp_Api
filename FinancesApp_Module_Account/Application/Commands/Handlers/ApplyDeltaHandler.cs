using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Commands.Handlers;
public class ApplyDeltaHandler : ICommandHandler<ApplyDelta, bool>
{
    private readonly ILogger<CreateAccountHandler> _logger;
    private readonly IEventStore _eventStore;

    public ApplyDeltaHandler(ILogger<CreateAccountHandler> logger, IEventStore eventStore)
    {
        _logger = logger;
        _eventStore = eventStore;
    }

    public async Task<bool> Handle(ApplyDelta command, CancellationToken cancellationToken = default)
    {
        var account = command.Account;

        try
        {
            _logger.LogInformation("Transction happening at {RequestedAt} for - ID: {AccountId}, UserId: {UserId}, Type: {Type}, Balance: {Balance}, Status: {Status}",
                                    command.RequestedAt, account.Id, account.UserId, account.Type, account.Balance, account.Status);

            var loaded = await _eventStore.Load(account.Id, token: cancellationToken);

            account.ApplyDelta(new Money(command.Value, command.Currency), command.OperationType);

            await _eventStore.Append(account.Id, account.GetUncommittedEvents(),account.CurrentVersion, default);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Transaction failed for - ID: {AccountId}, UserId: {UserId}, Type: {Type}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}",
                account.Id, account.UserId, account.Type, account.Balance, account.CreditLimit, account.CurrentDebt, account.Status);
            throw;
        }
    }
}
