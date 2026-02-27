using FinancesApp_Api.Contracts.Requests.AccountRequests;
using FinancesApp_Api.Endpoints;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_CQRS.Queries;
using FinancesApp_Module_Account.Application.Queries;
using FinancesApp_Module_Account.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FinancesApp_Api.Controllers;

[ApiController]
public class AccountController(IQueryHandler<GetAccounts, IReadOnlyList<Account>> getAccountsHandler,
                               IQueryHandler<GetAccountById, Account> getAccountByIdHandler,
                               IQueryHandler<GetActiveAccounts, IReadOnlyList<Account>> getActiveAccountsHandler,
                               ICommandHandler<CreateAccount, bool> createAccountHandler) : ControllerBase
{
    [HttpGet(AccountEndpoints.GetAccounts)]
    public async Task<IActionResult> GetAccounts(CancellationToken token = default)
    {

        var query = new GetAccounts();
        var accounts = await getAccountsHandler.Handle(query, token);

        return Ok(accounts);
    }

    [HttpGet(AccountEndpoints.GetAccountById)]
    public async Task<IActionResult> GetAccountById([FromRoute] string accountId, CancellationToken token = default)
    {

        if (!Guid.TryParse(accountId, out var accountGuid))
            return BadRequest("Invalid Id");

        var query = new GetAccountById()
        {
            AccountId = accountGuid
        };

        var account = await getAccountByIdHandler.Handle(query, token);

        if (account.Id == Guid.Empty)
            return NotFound();

        return Ok(account);
    }

    [HttpGet(AccountEndpoints.GetActiveAccounts)]
    public async Task<IActionResult> GetActiveAccounts(CancellationToken token = default)
    {
        var query = new GetActiveAccounts();
        var accounts = await getActiveAccountsHandler.Handle(query, token);
        return Ok(accounts);
    }

    [HttpPost(AccountEndpoints.CreateAccount)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, 
                                                    CancellationToken token = default)
    {

        var command = new CreateAccount()
        {
            Account = request.MapToAccount()
        };

        var result = await createAccountHandler.Handle(command, token);

        if (!result)
            return BadRequest("Failed to create account");

        return Ok("Account created successfully");
    }
}
