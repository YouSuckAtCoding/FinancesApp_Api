using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_User.Application.Queries.Handlers;
public class GetUsersHandler(IUserReadRepository userReadRepository,
                             ILogger<GetUserByIdHandler> logger) : IQueryHandler<GetUsers, IReadOnlyList<User>>
{
    private readonly IUserReadRepository _userReadRepository = userReadRepository;
    private readonly ILogger<GetUserByIdHandler> _logger = logger;

    public async Task<IReadOnlyList<User>> Handle(GetUsers query,
                                                  CancellationToken token = default)
    {
        try
        {
            var result = await _userReadRepository.GetUsers(token: token);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return [];
        }
    }
}
