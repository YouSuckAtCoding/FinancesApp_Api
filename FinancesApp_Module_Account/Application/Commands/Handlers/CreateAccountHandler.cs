using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Commands.Handlers;
public class CreateAccountHandler : ICommandHandler<CreateAccount, bool>
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<CreateAccountHandler> _logger;

    public CreateAccountHandler(IAccountRepository accountRepository,
                                ILogger<CreateAccountHandler> logger,
                                IEventStore eventStore)
    {
        _logger = logger;
        _eventStore = eventStore;
    }

    public async Task<bool> Handle(CreateAccount command, CancellationToken cancellationToken = default)
    {
        try
        { 
            await _eventStore.Append(command.Account.Id, command.Account.GetUncommittedEvents(),command.Account.CurrentVersion, default);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating account - ID: {AccountId}, UserId: {UserId},  Type: {Type}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}",
                command.Account.Id, command.Account.UserId, command.Account.Type, command.Account.Balance, command.Account.CreditLimit, command.Account.CurrentDebt, command.Account.Status);
            throw;
        }
    }
}
