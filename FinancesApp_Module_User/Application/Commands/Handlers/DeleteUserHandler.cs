using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_User.Application.Commands.Handlers;

public class DeleteUserHandler(IUserRepository userRepository, 
                               IUserReadRepository userReadRepository, 
                               ILogger<DeleteUserHandler> logger) : ICommandHandler<DeleteUser, bool>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUserReadRepository _userReadRepository = userReadRepository;
    private readonly ILogger<DeleteUserHandler> _logger = logger;

    public async Task<bool> Handle(DeleteUser command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to delete user with ID: {UserId}", command.UserId);

        try
        {
            var result = await _userRepository.DeleteUserAsync(command.UserId, token: cancellationToken);

            if (result)
            {
                _logger.LogInformation(
                    "User deleted successfully - ID: {UserId}",
                    command.UserId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to delete user - ID: {UserId}",
                    command.UserId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID {UserId}", command.UserId);
            return false;
        }
    }
}