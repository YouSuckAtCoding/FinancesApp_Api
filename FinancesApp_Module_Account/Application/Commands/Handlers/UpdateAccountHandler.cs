using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Commands.Handlers;
public class UpdateAccountHandler : ICommandHandler<UpdateAccount, bool>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<UpdateAccountHandler> _logger;
    private readonly IEventStore _eventStore;

    public UpdateAccountHandler(IAccountRepository accountRepository,
                                ILogger<UpdateAccountHandler> logger,
                                IEventStore eventStore)
    {
        _accountRepository = accountRepository;
        _logger = logger;
        _eventStore = eventStore;
    }

    public async Task<bool> Handle(UpdateAccount command, CancellationToken cancellationToken = default)
    {
        var account = new Account();

        try
        {
            var loaded = await _eventStore.Load(command.Account.Id, token: cancellationToken);

            account.RebuildFromEvents(loaded);

            await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion, default);
            
            return account.Id != Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating account - ID: {AccountId}, UserId: {UserId},  Type: {Type}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}",
                account.Id, account.UserId, account.Type, account.Balance, account.CreditLimit, account.CurrentDebt, account.Status);
            throw;
        }
    }
}
