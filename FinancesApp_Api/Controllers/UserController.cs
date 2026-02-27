using FinancesApp_Api.Contracts.Requests.UserRequests;
using FinancesApp_Api.Endpoints;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Commands;
using FinancesApp_Module_User.Application.Queries;
using FinancesApp_Module_User.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FinancesApp_Api.Contracts.Requests.UserRequests;

[ApiController]
public partial class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IQueryHandler<GetUsers, IReadOnlyList<User>> _getUsersHandler;
    private readonly IQueryHandler<GetUserById, User> _getUserByIdHandler;
    private readonly ICommandHandler<CreateUser, Guid> _createUserHandler;
    private readonly ICommandHandler<UpdateUser, bool> _updateUserHandler;

    public UserController(IQueryHandler<GetUsers,
                          IReadOnlyList<User>> getUsersHandler,
                          ILogger<UserController> logger,
                          IQueryHandler<GetUserById, User> getUserByIdHandler,
                          ICommandHandler<CreateUser, Guid> createUserHandler,
                          ICommandHandler<UpdateUser, bool> updateUserHandler)
    {
        _getUsersHandler = getUsersHandler;
        _logger = logger;
        _getUserByIdHandler = getUserByIdHandler;
        _createUserHandler = createUserHandler;
        _updateUserHandler = updateUserHandler;
    }

    [HttpGet(UserEndpoints.GetAll)]
    public async Task<IActionResult> GetUsers(CancellationToken token = default)
    {
        var query = new GetUsers();
        var result = await _getUsersHandler.Handle(query, token);

        return Ok(result);
    }

    [HttpGet(UserEndpoints.Get)]
    public async Task<IActionResult> GetById([FromRoute] string userId,
                                             CancellationToken token = default)
    {

        if (!Guid.TryParse(userId, out var userGuid))
            return BadRequest("Invalid Id");

        var query = new GetUserById()
        {
            UserId = userGuid
        };

        var result = await _getUserByIdHandler.Handle(query, token);

        return result.Id != Guid.Empty ? Ok(result) : NotFound();
    }

    [HttpPost(UserEndpoints.Create)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request,
                                            CancellationToken token = default)
    {
        var command = new CreateUser(request.MapToUser());  

        var result = await _createUserHandler.Handle(command, token);

        return result == Guid.Empty ? BadRequest() : Ok(result);

    }

    [HttpPut(UserEndpoints.Update)]
    public async Task<IActionResult> Update([FromBody] UpdateUserRequest request,
                                            CancellationToken token = default)
    {
        var command = new UpdateUser(request.MapToUser());

        var result = await _updateUserHandler.Handle(command, token);

        return result ? Ok(request.Id) : BadRequest();

    }
}
