using FinancesApp_CQRS.Commands;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_CQRS.Queries;
using FinancesApp_Module_Account.Application.Commands;
using FinancesApp_Module_Account.Application.Commands.Handlers;
using FinancesApp_Module_Account.Application.Queries;
using FinancesApp_Module_Account.Application.Queries.Handlers;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;

namespace FinancesApp_Api.StartUp;

public static class AccountModuleInjections
{
    public static IServiceCollection AddAccountModule(this IServiceCollection services)
    {
        services.AddScoped<IAccountReadRepository, AccountReadRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();

        services.AddScoped<ICommandHandler<CreateAccount, bool>, CreateAccountHandler>();
        services.AddScoped<ICommandHandler<UpdateAccount, bool>, UpdateAccountHandler>();
        services.AddScoped<ICommandHandler<DeleteAccount, bool>, DeleteAccountHandler>();

        services.AddScoped<IQueryHandler<GetAccountById, Account>, GetAccountByIdHandler>();
        services.AddScoped<IQueryHandler<GetAccounts, IReadOnlyList<Account>>, GetAccountsHandler>();
        services.AddScoped<IQueryHandler<GetActiveAccounts, IReadOnlyList<Account>>, GetActiveAccountsHandler>();

        return services;
    }
}
