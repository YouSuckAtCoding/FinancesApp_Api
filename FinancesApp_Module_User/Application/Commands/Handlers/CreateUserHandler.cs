using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace FinancesApp_Module_User.Application.Commands.Handlers;

public class CreateUserHandler(IUserRepository userRepository, 
                               ILogger<CreateUserHandler> logger) : ICommandHandler<CreateUser, Guid>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<CreateUserHandler> _logger = logger;

    public async Task<Guid> Handle(CreateUser command, CancellationToken cancellationToken = default)
    {
        var user = command.User;

        _logger.LogInformation(
            "Creating user - ID: {UserId}, Name: {Name}, Email: {Email}, DateOfBirth: {DateOfBirth}, RegisteredAt: {RegisteredAt}",
            user.Id, user.Name, user.Email, user.DateOfBirth, user.RegisteredAt);

        try
        {
            var result = await _userRepository.CreateUserAsync(user, cancellationToken);

            if (result != Guid.Empty)
            {
                _logger.LogInformation(
                    "User created successfully - ID: {UserId}, Name: {Name}, Email: {Email}, RegisteredAt: {RegisteredAt}, ProfileImage: {ProfileImage}",
                    user.Id, user.Name, user.Email, user.RegisteredAt, user.ProfileImage);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to create user - ID: {UserId}, Name: {Name}, Email: {Email}",
                    user.Id, user.Name, user.Email);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with ID {UserId}", user.Id);
            return Guid.Empty;
        }
    }
}