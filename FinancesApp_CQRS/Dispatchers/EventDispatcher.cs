using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_CQRS.Dispatchers;
public class EventDispatcher : IEventDispatcher
{
    private readonly Dictionary<Type, List<Func<IDomainEvent, CancellationToken, Task>>> _handlers = [];

    public void Register<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        var type = typeof(TEvent);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = [];

        _handlers[type].Add((evt, token) => handler.HandleAsync((TEvent)evt, token));
    }

    public async Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken token = default)
    {
        foreach (var evt in events)
            await Dispatch(evt, token);
    }

    public async Task Dispatch(IDomainEvent evt, CancellationToken token = default)
    {
        if (_handlers.TryGetValue(evt.GetType(), out var handlers))
            foreach (var handler in handlers)
                await handler(evt, token);
    }
}
