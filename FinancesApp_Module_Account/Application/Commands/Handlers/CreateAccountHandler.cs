using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Commands.Handlers;
public class CreateAccountHandler : ICommandHandler<CreateAccount, bool>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<CreateAccountHandler> _logger;

    public CreateAccountHandler(IAccountRepository accountRepository,
                                ILogger<CreateAccountHandler> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(CreateAccount command, CancellationToken cancellationToken = default)
    {
        var account = command.Account;

        _logger.LogInformation(
            "Creating account - ID: {AccountId}, UserId: {UserId}, Name: {Name}, Type: {Type}, Balance: {Balance}, Status: {Status}",
            account.Id, account.UserId, account.Name, account.Type, account.Balance, account.Status);

        try
        {
            var result = await _accountRepository.CreateAccountAsync(account, token: cancellationToken);

            if (result)
            {
                _logger.LogInformation(
                    "Account created successfully - ID: {AccountId}, Name: {Name}, Type: {Type}, Balance: {Balance}, CreditLimit: {CreditLimit}, CreatedAt: {CreatedAt}",
                    account.Id, account.Name, account.Type, account.Balance, account.CreditLimit, account.CreatedAt);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to create account - ID: {AccountId}, Name: {Name}, Type: {Type}",
                    account.Id, account.Name, account.Type);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating account - ID: {AccountId}, UserId: {UserId}, Name: {Name}, Type: {Type}, Balance: {Balance}, CreditLimit: {CreditLimit}, CurrentDebt: {CurrentDebt}, Status: {Status}",
                account.Id, account.UserId, account.Name, account.Type, account.Balance, account.CreditLimit, account.CurrentDebt, account.Status);
            throw;
        }
    }
}
