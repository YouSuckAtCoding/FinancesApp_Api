using FinancesApp_CQRS.Commands;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Commands;
public class DeleteAccountHandler : ICommandHandler<DeleteAccount, bool>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountReadRepository _accountReadRepository;
    private readonly ILogger<DeleteAccountHandler> _logger;

    public DeleteAccountHandler(IAccountRepository accountRepository,
                                IAccountReadRepository accountReadRepository,
                                ILogger<DeleteAccountHandler> logger)
    {
        _accountRepository = accountRepository;
        _accountReadRepository = accountReadRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteAccount command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to delete account - ID: {AccountId}", command.AccountId);

        try
        {             
            var account = await _accountReadRepository.GetAccountById(
                command.AccountId,
                token: cancellationToken);

            if (account.Id != Guid.Empty)
            {
                _logger.LogInformation(
                    "Account found for deletion - ID: {AccountId}, UserId: {UserId}, Name: {Name}, Type: {Type}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}, CreatedAt: {CreatedAt}, ClosedAt: {ClosedAt}",
                    account.Id, account.UserId, account.Name, account.Type, account.Balance,
                    account.CreditLimit, account.CurrentDebt, account.Status,
                    account.CreatedAt, account.ClosedAt);
            }

            var result = await _accountRepository.DeleteAccountAsync(
                command.AccountId,
                token: cancellationToken);

            if (result)
            {
                _logger.LogInformation(
                    "Account deleted successfully - ID: {AccountId}, Name: {Name}, Type: {Type}, Balance: {Balance}, Status: {Status}",
                    account.Id, account.Name, account.Type, account.Balance, account.Status);
            }
            else
            {
                _logger.LogWarning(
                    "Account {AccountId} not found for deletion or deletion failed",
                    command.AccountId);
            }

            return result;
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