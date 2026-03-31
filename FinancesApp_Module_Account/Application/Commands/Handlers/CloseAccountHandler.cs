using FinancesApp_CQRS.Commands;
using FinancesApp_CQRS.EventStore;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Commands.Handlers;
public class CloseAccountHandler : ICommandHandler<DeleteAccount, bool>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IEventStore _eventStore;
    private readonly ILogger<CloseAccountHandler> _logger;

    public CloseAccountHandler(IAccountRepository accountRepository,
                               IAccountReadRepository accountReadRepository,
                               ILogger<CloseAccountHandler> logger,
                               IEventStore eventStore)
    {
        _accountRepository = accountRepository;
        _logger = logger;
        _eventStore = eventStore;
    }

    public async Task<bool> Handle(DeleteAccount command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to delete account - ID: {AccountId}", command.AccountId);

        var account = new Account();

        try
        {
            var loaded = await _eventStore.Load(command.AccountId, token: cancellationToken);

            account.RebuildFromEvents(loaded);

            account.Close();

            await _eventStore.Append(account.Id, account.GetUncommittedEvents(), account.CurrentVersion, default);

            return account.Id != Guid.Empty;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deleting account - ID: {AccountId}",
                command.AccountId);
            throw;
        }
    }
}