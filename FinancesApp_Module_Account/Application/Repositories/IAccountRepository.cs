using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;
using Microsoft.Data.SqlClient;

public interface IAccountRepository
{
    Task<bool> CreateAccountAsync(Account account, SqlConnection? connection = null, CancellationToken token = default);
    Task<bool> DeleteAccountAsync(Guid accountId, SqlConnection? connection = null, CancellationToken token = default);
    Task<bool> UpdateAccountAsync(Account account, SqlConnection? connection = null, CancellationToken token = default);

    // Projection-targeted updates — one method per mutation type
    Task ApplyDepositAsync(Guid accountId, Money amount, CancellationToken token = default);
    Task ApplyWithdrawAsync(Guid accountId, Money amount, CancellationToken token = default);
    Task UpdateDebtAsync(Guid accountId, Money newDebt, CancellationToken token = default);
    Task UpdateCreditLimitAsync(Guid accountId, Money creditLimit, CancellationToken token = default);
    Task ApplyPaymentAsync(Guid accountId, Money paymentAmount, CancellationToken token = default);
    Task UpdateDueDateAsync(Guid accountId, DateTimeOffset dueDate, CancellationToken token = default);
    Task CloseAccountAsync(Guid accountId, DateTimeOffset closedAt, CancellationToken token = default);
    Task SyncStateAsync(Guid accountId, Money balance, Money debt, AccountType type, CancellationToken token = default);
}