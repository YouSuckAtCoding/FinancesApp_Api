using FinancesApp_Module_Account.Domain;

public interface IAccountRepository
{
    Task<bool> CreateAccountAsync(Account account, CancellationToken token = default);
    Task<bool> DeleteAccountAsync(Guid accountId, CancellationToken token = default);
    Task<bool> UpdateAccountAsync(Account account, CancellationToken token = default);
}