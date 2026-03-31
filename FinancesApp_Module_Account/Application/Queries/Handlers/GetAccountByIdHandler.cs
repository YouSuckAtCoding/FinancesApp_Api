using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Application.Queries;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace FinancesApp_Module_Account.Application.Queries.Handlers;
public class GetAccountByIdHandler(IEventStore eventStore,
                                     ILogger<GetAccountByIdHandler> logger) : IQueryHandler<GetAccountById, Account>
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly ILogger<GetAccountByIdHandler> _logger = logger;

    public async Task<Account> Handle(GetAccountById query, 
                                      CancellationToken token = default)
    {
        var result = new Account(); 
        try
        {
            var loaded = await _eventStore.Load(query.AccountId, token: token);

            result.RebuildFromEvents(loaded);

            return result;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account with ID {AccountId}", query.AccountId);
            return new Account(); 
        }
    }
}
