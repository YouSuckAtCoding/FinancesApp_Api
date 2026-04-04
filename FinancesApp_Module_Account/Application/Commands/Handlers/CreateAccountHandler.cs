using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_Account.Application.Commands.Handlers;
public class CreateAccountHandler : ICommandHandler<CreateAccount, bool>
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<CreateAccountHandler> _logger;
    private readonly IAccountRepository _accountRepository;

    private static readonly Counter AccountsCreated = Metrics
    .CreateCounter("account_total_CreateAccount", "Total number of accounts created");

    private static readonly Histogram AccountCreationDuration = Metrics
        .CreateHistogram("account_creation_duration_seconds", "Account creation processing time",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public CreateAccountHandler(IAccountRepository accountRepository,
                                ILogger<CreateAccountHandler> logger,
                                IEventStore eventStore)
    {
        _accountRepository = accountRepository;
        _logger = logger;
        _eventStore = eventStore;
    }

    public async Task<bool> Handle(CreateAccount command, CancellationToken cancellationToken = default)
    {
        using (AccountCreationDuration.NewTimer())
        {
            try
            {               
                await _eventStore.Append(command.Account.Id, command.Account.GetUncommittedEvents(), command.Account.CurrentVersion, default);

                await _accountRepository.CreateAccountAsync(command.Account, token: cancellationToken); 
                
                AccountsCreated.Inc();

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
}
