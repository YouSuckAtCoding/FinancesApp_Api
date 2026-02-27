using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Commands.Handlers;
using FinancesApp_Module_Credentials.Application.Queries;
using FinancesApp_Module_Credentials.Application.Queries.Handlers;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Api.StartUp;

public static class CredentialsModuleInjections
{
    public static IServiceCollection AddCredentialsModule(this IServiceCollection services)
    {
        services.AddScoped<IUserCredentialsReadRepository, UserCredentialsReadRepository>();
        services.AddScoped<IUserCredentialsRepository, UserCredentialsRepository>();

        services.AddScoped<ICommandHandler<RegisterUserCredentials, Guid>, RegisterUserCredentialsHandler>();
        services.AddScoped<ICommandHandler<UpdateUserCredentials, bool>, UpdateUserCredentialsHandler>();
        services.AddScoped<ICommandHandler<DeleteUserCredentials, bool>, DeleteUserCredentialsHandler>();

        services.AddScoped<IQueryHandler<GetUserCredentialsByLogin, UserCredentials>, GetUserCredentialsByLoginHandler>();
        services.AddScoped<IQueryHandler<GetUserCredentialsByUserId, UserCredentials>, GetUserCredentialsByUserIdHandler>();

        return services;
    }
}
