using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Commands.Handlers;
public class CreateAccountHandler : ICommandHandler<CreateAccount, bool>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IEventStore _eventStore;
    private readonly ILogger<CreateAccountHandler> _logger;

    public CreateAccountHandler(IAccountRepository accountRepository,
                                ILogger<CreateAccountHandler> logger,
                                IAccountReadRepository accountReadRepository,
                                IEventStore eventStore)
    {
        _accountRepository = accountRepository;
        _logger = logger;
        _eventStore = eventStore;
    }

    public async Task<bool> Handle(CreateAccount command, CancellationToken cancellationToken = default)
    {
        var account = new Account();

        try
        {
            var loaded = await _eventStore.Load(account.Id, token: cancellationToken);

            account.RebuildFromEvents(loaded);

            await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion,  cancellationToken);

            return true;
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
