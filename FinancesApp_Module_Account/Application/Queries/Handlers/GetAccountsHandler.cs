using FinancesApp_CQRS.Interfaces;
using FinancesApp_CQRS.Queries;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Queries.Handlers;
public class GetAccountsHandler(IAccountReadRepository accountRepository,
                                ILogger<GetAccountsHandler> logger) : IQueryHandler<GetAccounts, IReadOnlyList<Account>>
{

    private readonly IAccountReadRepository _accountRepository = accountRepository;
    private readonly ILogger<GetAccountsHandler> _logger = logger;

    public async Task<IReadOnlyList<Account>> Handle(GetAccounts query, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _accountRepository.GetAccounts(token: cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts");
            return [];
        }
    }
}
