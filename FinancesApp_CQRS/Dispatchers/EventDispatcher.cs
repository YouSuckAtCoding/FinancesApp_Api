using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_CQRS.Dispatchers;
public class EventDispatcher
{
    private readonly Dictionary<Type, List<object>> _handlers = [];

    public void Register<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        var type = typeof(TEvent);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = [];
        _handlers[type].Add(handler);
    }

    public void Dispatch(IEnumerable<IDomainEvent> events)
    {
        foreach (var evt in events)
        {
            if (_handlers.TryGetValue(evt.GetType(), out var handlers))
                foreach (var handler in handlers)
                    ((dynamic)handler).Handle((dynamic)evt);
        }
    }
}
