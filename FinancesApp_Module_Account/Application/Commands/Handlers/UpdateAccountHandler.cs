using FinancesApp_CQRS.Interfaces;
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

    public async Task<bool> Handle(UpdateAccount command, 
                                   CancellationToken cancellationToken = default)
    {
        var account = command.Account;

        _logger.LogInformation(
            "Updating account - ID: {AccountId}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}, PaymentDate: {PaymentDate}, DueDate: {DueDate}, ClosedAt: {ClosedAt}",
            account.Id,  account.Balance, account.CreditLimit, account.CurrentDebt,
            account.Status, account.PaymentDate, account.DueDate, account.ClosedAt);

        try
        {

            var events = _eventStore.Load(command.Account.Id, token: cancellationToken);
            
            var result = await _accountRepository.UpdateAccountAsync(account, token: cancellationToken);

            if (result)
            {
                _logger.LogInformation(
                    "Account updated successfully - ID: {AccountId}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}, ClosedAt: {ClosedAt}",
                    account.Id,  account.Balance, account.CreditLimit, account.CurrentDebt,
                    account.Status, account.ClosedAt);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to update account - ID: {AccountId}",
                    account.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating account - ID: {AccountId}, UserId: {UserId}, Type: {Type}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}, PaymentDate: {PaymentDate}, DueDate: {DueDate}, CreatedAt: {CreatedAt}, ClosedAt: {ClosedAt}",
                account.Id, account.UserId,  account.Type, account.Balance, account.CreditLimit,
                account.CurrentDebt, account.Status, account.PaymentDate, account.DueDate,
                account.CreatedAt, account.ClosedAt);
            throw;
        }
    }
}
