using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Queries;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_Account.Application.Queries.Handlers;
public class GetActiveAccountsHandler(IAccountReadRepository accountRepository,
                                        ILogger<GetActiveAccountsHandler> logger) : IQueryHandler<GetActiveAccounts, IReadOnlyList<Account>>
{
    private readonly IAccountReadRepository _accountRepository = accountRepository;
    private readonly ILogger<GetActiveAccountsHandler> _logger = logger;
    public async Task<IReadOnlyList<Account>> Handle(GetActiveAccounts query, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _accountRepository.GetActiveAccounts(token: cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active accounts");
            return [];
        }
    }
}
