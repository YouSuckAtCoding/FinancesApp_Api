using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Commands;
using FinancesApp_Module_User.Application.Commands.Handlers;
using FinancesApp_Module_User.Application.Queries;
using FinancesApp_Module_User.Application.Queries.Handlers;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;

namespace FinancesApp_Api.StartUp;

public static class UserModuleInjections
{
    public static IServiceCollection AddUserModule(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserReadRepository, UserReadRepository>();

        services.AddScoped<ICommandHandler<CreateUser, Guid>, CreateUserHandler>();
        services.AddScoped<ICommandHandler<UpdateUser, bool>, UpdateUserHandler>();
        services.AddScoped<ICommandHandler<DeleteUser, bool>, DeleteUserHandler>();

        services.AddScoped<IQueryHandler<GetUserById, User>, GetUserByIdHandler>();
        services.AddScoped<IQueryHandler<GetUsers, IReadOnlyList<User>>, GetUsersHandler>();

        return services;
    }
}
