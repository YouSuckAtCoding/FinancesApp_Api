using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.Events;

namespace FinancesApp_Module_Account.Application;

public class AccountProjection(IAccountRepository accountRepository, IProjectionCheckpoint checkpoint) :
    IEventHandler<AccountCreatedEvent>,
    IEventHandler<DepositEvent>,
    IEventHandler<WithdrawEvent>,
    IEventHandler<CreditUpdatedEvent>,
    IEventHandler<AccountClosedEvent>,
    IEventHandler<UpdatedAccountEvent>,
    IEventHandler<CalculatedCreditLimitEvent>,
    IEventHandler<CredidCardStatementPaymentEvent>,
    IEventHandler<DebtRecalculatedEvent>
{
    public async Task HandleAsync(AccountCreatedEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await accountRepository.CreateAccountAsync(new Account(
            evt.Id, evt.UserId, evt.Balance, evt.Type), token: token);
    }

    public async Task HandleAsync(DepositEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await accountRepository.ApplyDepositAsync(evt.AccountId, evt.Amount, token);
    }

    public async Task HandleAsync(WithdrawEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await accountRepository.ApplyWithdrawAsync(evt.AccountId, evt.Amount, token);
    }

    public async Task HandleAsync(CreditUpdatedEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await accountRepository.UpdateDebtAsync(evt.AccountId, evt.NewDebt, token);
    }

    public async Task HandleAsync(AccountClosedEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await accountRepository.CloseAccountAsync(evt.AccountId, evt.Timestamp, token);
    }

    public async Task HandleAsync(UpdatedAccountEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await accountRepository.SyncStateAsync(evt.Id, evt.balance, evt.debt, evt.type, token);
    }

    public async Task HandleAsync(CalculatedCreditLimitEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await accountRepository.UpdateCreditLimitAsync(evt.AccountId, evt.Value, token);
    }

    public async Task HandleAsync(CredidCardStatementPaymentEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await accountRepository.ApplyPaymentAsync(evt.AccountId, evt.Amount, token);
    }

    public async Task HandleAsync(DebtRecalculatedEvent evt, CancellationToken token = default)
    {
        if (!await checkpoint.TryClaimAsync(evt.EventId, token)) return;
        await accountRepository.UpdateDueDateAsync(evt.AccountId, evt.DueDate, token);
    }
}
