using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_User.Application.Queries.Handlers;
public class GetUserByEmailHandler(IUserReadRepository userReadRepository,
                                  ILogger<GetUserByEmailHandler> logger) : IQueryHandler<GetUserByEmail, User>
{
    private readonly IUserReadRepository _userReadRepository = userReadRepository;
    private readonly ILogger<GetUserByEmailHandler> _logger = logger;

    public async Task<User> Handle(GetUserByEmail query,
                                   CancellationToken token = default)
    {
        try
        {
            var result = await _userReadRepository.GetUserByEmail(query.Email, token: token);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with Email {Email}", query.Email);
            return new User();
        }
    }
}
