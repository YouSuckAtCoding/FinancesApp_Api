using FinancesApp_Api.Contracts.Requests.CredentialsRequests;
using FinancesApp_Api.Endpoints;
using FinancesApp_Api.Mapper;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Queries;
using FinancesApp_Module_Credentials.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FinancesApp_Api.Controllers;

[ApiController]
public class UserCredentialsController(IQueryHandler<GetUserCredentialsByUserId, UserCredentials> getByUserIdHandler,
                                       IQueryHandler<GetUserCredentialsByLogin, UserCredentials> getByLoginHandler,
                                       ICommandHandler<RegisterUserCredentials, Guid> createCredentialsHandler,
                                       ICommandHandler<UpdateUserCredentials, bool> updateCredentialsHandler,
                                       ICommandHandler<DeleteUserCredentials, bool> deleteCredentialsHandler) : ControllerBase
{
    [HttpGet(CredentialsEndpoints.GetByUserId)]
    public async Task<IActionResult> GetByUserId([FromRoute] string userId, CancellationToken token = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return BadRequest("Invalid Id");

        var query = new GetUserCredentialsByUserId 
        { 
            UserId = userGuid 
        };

        var result = await getByUserIdHandler.Handle(query, token);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet(CredentialsEndpoints.GetByLogin)]
    public async Task<IActionResult> GetByLogin([FromRoute] string login, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(login))
            return BadRequest("Login cannot be empty");

        var query = new GetUserCredentialsByLogin {
            Login = login 
        };

        var result = await getByLoginHandler.Handle(query, token);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    //[HttpPost(CredentialsEndpoints.Login)]
    //public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken token = default)
    //{
    //    var query = new GetCredentialsByLogin {
    //        Login = request.Login,
    //        Password = request.PlainPassword
    //    };

    //    var credentials = await _getByLoginHandler.Handle(query, token);

    //    if (credentials is null || !credentials.VerifyPassword(request.PlainPassword))
    //        return Unauthorized();

    //    var jwtToken = _tokenService.GenerateToken(credentials);

    //    return Ok(new { Token = jwtToken });
    //}

    [HttpPost(CredentialsEndpoints.CreateCredentials)]
    public async Task<IActionResult> CreateCredentials([FromBody] CreateCredentialsRequest request,
                                                        CancellationToken token = default)
    {
        var command = new RegisterUserCredentials(request.MapToUserCredentials());
        var result = await createCredentialsHandler.Handle(command, token);

        if (result == Guid.Empty)
            return BadRequest("Failed to create credentials");

        return Ok(result);
    }

    [HttpPut(CredentialsEndpoints.UpdateCredentials)]
    public async Task<IActionResult> UpdateCredentials([FromBody] UpdateCredentialsRequest request,
                                                       CancellationToken token = default)
    {
        if (!Guid.TryParse(request.UserId, out var userGuid))
            return BadRequest("Invalid Id");

        var command = new UpdateUserCredentials(userGuid, request.NewPlainPassword);
        var result = await updateCredentialsHandler.Handle(command, token);

        if (!result)
            return BadRequest("Failed to update credentials");

        return Ok("Credentials updated successfully");
    }

    [HttpDelete(CredentialsEndpoints.DeleteCredentials)]
    public async Task<IActionResult> DeleteCredentials([FromRoute] string userId, CancellationToken token = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return BadRequest("Invalid Id");

        var command = new DeleteUserCredentials(userGuid);
        var result = await deleteCredentialsHandler.Handle(command, token);

        if (!result)
            return BadRequest("Failed to delete credentials");

        return Ok("Credentials deleted successfully");
    }
}

