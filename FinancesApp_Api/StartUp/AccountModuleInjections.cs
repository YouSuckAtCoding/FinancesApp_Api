using FinancesApp_CQRS.Commands;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_CQRS.Projections;
using FinancesApp_CQRS.Queries;
using FinancesApp_Module_Account.Application;
using FinancesApp_Module_Account.Application.Commands;
using FinancesApp_Module_Account.Application.Commands.Handlers;
using FinancesApp_Module_Account.Application.Queries;
using FinancesApp_Module_Account.Application.Queries.Handlers;
using FinancesApp_Module_Account.Application.Repositories;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.Events;

namespace FinancesApp_Api.StartUp;

public static class AccountModuleInjections
{
    public static IServiceCollection AddAccountModule(this IServiceCollection services)
    {
        services.AddSingleton<IAccountReadRepository, AccountReadRepository>();
        services.AddSingleton<IAccountRepository, AccountRepository>();
        services.AddSingleton<IProjectionCheckpoint, ProjectionCheckpoint>();
        services.AddSingleton<AccountProjection>();

        services.AddScoped<ICommandHandler<CreateAccount, bool>, CreateAccountHandler>();
        services.AddScoped<ICommandHandler<UpdateAccount, bool>, UpdateAccountHandler>();
        services.AddScoped<ICommandHandler<DeleteAccount, bool>, DeleteAccountHandler>();
        services.AddScoped<ICommandHandler<ApplyDelta, bool>, ApplyDeltaHandler>();

        services.AddScoped<IQueryHandler<GetAccountById, Account>, GetAccountByIdHandler>();
        services.AddScoped<IQueryHandler<GetAccounts, IReadOnlyList<Account>>, GetAccountsHandler>();
        services.AddScoped<IQueryHandler<GetActiveAccounts, IReadOnlyList<Account>>, GetActiveAccountsHandler>();

        return services;
    }

    public static WebApplication AddAccountProjections(this WebApplication app)
    {
        var dispatcher = app.Services.GetRequiredService<IEventDispatcher>();
        var projection = app.Services.GetRequiredService<AccountProjection>();

        dispatcher.Register<AccountCreatedEvent>(projection);
        dispatcher.Register<DepositEvent>(projection);
        dispatcher.Register<WithdrawEvent>(projection);
        dispatcher.Register<CreditUpdatedEvent>(projection);
        dispatcher.Register<AccountClosedEvent>(projection);
        dispatcher.Register<UpdatedAccountEvent>(projection);
        dispatcher.Register<CalculatedCreditLimitEvent>(projection);
        dispatcher.Register<CredidCardStatementPaymentEvent>(projection);
        dispatcher.Register<DebtRecalculatedEvent>(projection);

        return app;
    }
}
