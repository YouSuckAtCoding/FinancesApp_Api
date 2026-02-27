using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Commands.Handlers;
public class UpdateAccountHandler : ICommandHandler<UpdateAccount, bool>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<UpdateAccountHandler> _logger;

    public UpdateAccountHandler(IAccountRepository accountRepository,
                                ILogger<UpdateAccountHandler> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateAccount command, CancellationToken cancellationToken = default)
    {
        var account = command.Account;

        _logger.LogInformation(
            "Updating account - ID: {AccountId}, Name: {Name}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}, PaymentDate: {PaymentDate}, DueDate: {DueDate}, ClosedAt: {ClosedAt}",
            account.Id, account.Name, account.Balance, account.CreditLimit, account.CurrentDebt,
            account.Status, account.PaymentDate, account.DueDate, account.ClosedAt);

        try
        {
            var result = await _accountRepository.UpdateAccountAsync(account, token: cancellationToken);

            if (result)
            {
                _logger.LogInformation(
                    "Account updated successfully - ID: {AccountId}, Name: {Name}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}, ClosedAt: {ClosedAt}",
                    account.Id, account.Name, account.Balance, account.CreditLimit, account.CurrentDebt,
                    account.Status, account.ClosedAt);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to update account - ID: {AccountId}, Name: {Name}",
                    account.Id, account.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating account - ID: {AccountId}, UserId: {UserId}, Name: {Name}, Type: {Type}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}, PaymentDate: {PaymentDate}, DueDate: {DueDate}, CreatedAt: {CreatedAt}, ClosedAt: {ClosedAt}",
                account.Id, account.UserId, account.Name, account.Type, account.Balance, account.CreditLimit,
                account.CurrentDebt, account.Status, account.PaymentDate, account.DueDate,
                account.CreatedAt, account.ClosedAt);
            throw;
        }
    }
}
