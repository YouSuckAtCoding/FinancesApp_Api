using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FinancesApp_CQRS.Dispatchers;
public class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Task<TResult> Dispatch<TCommand, TResult>(TCommand command, CancellationToken cancellation) where TCommand : ICommand<TResult>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return handler.Handle(command, cancellation);
    }

}
