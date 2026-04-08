using FinancesApp_CQRS.Interfaces;
using FinancesApp_CQRS.Projections;
using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Commands.Handlers;
using FinancesApp_Module_Credentials.Application.Projections;
using FinancesApp_Module_Credentials.Application.Queries;
using FinancesApp_Module_Credentials.Application.Queries.Handlers;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_Credentials.Domain.Events;

namespace FinancesApp_Api.StartUp;

public static class CredentialsModuleInjections
{
    public static IServiceCollection AddCredentialsModule(this IServiceCollection services)
    {
        services.AddSingleton<IUserCredentialsRepository, UserCredentialsRepository>();
        services.AddSingleton<IUserCredentialsReadRepository, UserCredentialsReadRepository>();
        services.AddSingleton<CredentialsProjection>();

        services.AddScoped<ICommandHandler<RegisterUserCredentials, Guid>, RegisterUserCredentialsHandler>();
        services.AddScoped<ICommandHandler<UpdateUserCredentials, bool>, UpdateUserCredentialsHandler>();
        services.AddScoped<ICommandHandler<DeleteUserCredentials, bool>, DeleteUserCredentialsHandler>();

        services.AddScoped<IQueryHandler<GetUserCredentialsByLogin, UserCredentials>, GetUserCredentialsByLoginHandler>();
        services.AddScoped<IQueryHandler<GetUserCredentialsByUserId, UserCredentials>, GetUserCredentialsByUserIdHandler>();

        return services;
    }

    public static WebApplication AddCredentialsProjections(this WebApplication app)
    {
        var dispatcher = app.Services.GetRequiredService<IEventDispatcher>();
        var projection = app.Services.GetRequiredService<CredentialsProjection>();

        dispatcher.Register<CredentialsRegisteredEvent>(projection);
        dispatcher.Register<CredentialsPasswordChangedEvent>(projection);
        dispatcher.Register<CredentialsDeletedEvent>(projection);

        return app;
    }
}
