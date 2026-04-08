using FinancesApp_CQRS.Interfaces;
using FinancesApp_CQRS.Projections;
using FinancesApp_Module_User.Application.Commands;
using FinancesApp_Module_User.Application.Commands.Handlers;
using FinancesApp_Module_User.Application.Projections;
using FinancesApp_Module_User.Application.Queries;
using FinancesApp_Module_User.Application.Queries.Handlers;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using FinancesApp_Module_User.Domain.Events;

namespace FinancesApp_Api.StartUp;

public static class UserModuleInjections
{
    public static IServiceCollection AddUserModule(this IServiceCollection services)
    {
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IUserReadRepository, UserReadRepository>();
        services.AddSingleton<UserProjection>();

        services.AddScoped<ICommandHandler<CreateUser, Guid>, CreateUserHandler>();
        services.AddScoped<ICommandHandler<UpdateUser, bool>, UpdateUserHandler>();
        services.AddScoped<ICommandHandler<DeleteUser, bool>, DeleteUserHandler>();

        services.AddScoped<IQueryHandler<GetUserById, User>, GetUserByIdHandler>();
        services.AddScoped<IQueryHandler<GetUsers, IReadOnlyList<User>>, GetUsersHandler>();
        services.AddScoped<IQueryHandler<GetUserByEmail, User>, GetUserByEmailHandler>();

        return services;
    }

    public static WebApplication AddUserProjections(this WebApplication app)
    {
        var dispatcher = app.Services.GetRequiredService<IEventDispatcher>();
        var projection = app.Services.GetRequiredService<UserProjection>();

        dispatcher.Register<UserCreatedEvent>(projection);
        dispatcher.Register<UserUpdatedEvent>(projection);
        dispatcher.Register<UserDeletedEvent>(projection);

        return app;
    }
}
