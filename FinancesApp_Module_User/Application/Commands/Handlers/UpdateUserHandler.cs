using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_User.Application.Commands.Handlers;

public class UpdateUserHandler(IUserRepository userRepository, 
                               ILogger<UpdateUserHandler> logger) : ICommandHandler<UpdateUser, bool>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<UpdateUserHandler> _logger = logger;

    public async Task<bool> Handle(UpdateUser command, CancellationToken cancellationToken = default)
    {
        var user = command.User;

        _logger.LogInformation(
            "Updating user - ID: {UserId}, Name: {Name}, Email: {Email}, ModifiedAt: {ModifiedAt}",
            user.Id, user.Name, user.Email, user.ModifiedAt);

        try
        {
            var result = await _userRepository.UpdateUserAsync(user, cancellationToken);

            if (result)
            {
                _logger.LogInformation(
                    "User updated successfully - ID: {UserId}, Name: {Name}, Email: {Email}, ModifiedAt: {ModifiedAt}, DateOfBirth: {DateOfBirth}, ProfileImage: {ProfileImage}",
                    user.Id, user.Name, user.Email, user.ModifiedAt, user.DateOfBirth, user.ProfileImage);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to update user - ID: {UserId}, Name: {Name}, Email: {Email}",
                    user.Id, user.Name, user.Email);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID {UserId}", user.Id);
            return false;
        }
    }
}