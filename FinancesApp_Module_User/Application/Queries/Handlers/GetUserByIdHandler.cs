using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_User.Application.Queries.Handlers;
public class GetUserByIdHandler(IUserReadRepository userReadRepository,
                                  ILogger<GetUserByIdHandler> logger) : IQueryHandler<GetUserById, User>
{
    private readonly IUserReadRepository _userReadRepository = userReadRepository;
    private readonly ILogger<GetUserByIdHandler> _logger = logger;

    public async Task<User> Handle(GetUserById query,
                                   CancellationToken token = default)
    {
        try
        {
            var result = await _userReadRepository.GetUserById(query.UserId, token: token);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", query.UserId);
            return new User();
        }
    }

}
