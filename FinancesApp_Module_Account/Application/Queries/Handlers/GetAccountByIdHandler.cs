using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Queries;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Queries.Handlers;
public class GetAccountByIdHandler(IAccountReadRepository accountRepository,
                                     ILogger<GetAccountByIdHandler> logger) : IQueryHandler<GetAccountById, Account>
{
    private readonly IAccountReadRepository _accountRepository = accountRepository;
    private readonly ILogger<GetAccountByIdHandler> _logger = logger;

    public async Task<Account> Handle(GetAccountById query, 
                                      CancellationToken token = default)
    {
        try
        {
            var result = await _accountRepository.GetAccountById(query.AccountId, token: token);
            return result;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account with ID {AccountId}", query.AccountId);
            return new Account(); 
        }
    }
}
