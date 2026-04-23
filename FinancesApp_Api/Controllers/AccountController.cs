using FinancesApp_Api.Contracts.Requests.AccountRequests;
using FinancesApp_Api.Endpoints;
using FinancesApp_Api.Mapper;
using FinancesApp_Api.StartUp;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_CQRS.Queries;
using FinancesApp_Module_Account.Application.Commands;
using FinancesApp_Module_Account.Application.Queries;
using FinancesApp_Module_Account.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinancesApp_Api.Controllers;

[Authorize]
[ApiController]
public class AccountController(IQueryHandler<GetAccounts, IReadOnlyList<Account>> getAccountsHandler,
                               IQueryHandler<GetAccountById, Account> getAccountByIdHandler,
                               IQueryHandler<GetActiveAccounts, IReadOnlyList<Account>> getActiveAccountsHandler,
                               ICommandHandler<CreateAccount, bool> createAccountHandler,
                               ICommandHandler<ApplyDelta, bool> applyDeltaHandler) : ControllerBase
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

    [EnableRateLimiting(RateLimitingInjections.DeltaPolicy)]
    [HttpPost(AccountEndpoints.ApplyDeltaEndpoint)]
    public async Task<IActionResult> ApplyDelta([FromBody] ApplyDeltaRequest request,
                                                CancellationToken token = default)
    {

        if (!Guid.TryParse(request.AccountId, out var accountGuid))
            return BadRequest("Invalid Id");

        var query = new GetAccountById()
        {
            AccountId = accountGuid
        };

        var account = await getAccountByIdHandler.Handle(query, token);

        if(account.Id == Guid.Empty)
            return NotFound();

        var command = new ApplyDelta()
        {
            Account = account,
            Value = request.Amount,
            Currency = request.Currency,
            OperationType = (OperationType)request.OperationType,
            RequestedAt = MappingUtils.ParseToDateTimeOffset(request.RequestedAt) ?? DateTimeOffset.UtcNow
        };
        var result = await applyDeltaHandler.Handle(command, token);
        
        if (!result)
            return BadRequest("Failed to apply delta");

        return Ok("Delta applied successfully");
    }
}

