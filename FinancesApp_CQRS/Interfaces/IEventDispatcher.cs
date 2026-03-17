namespace FinancesApp_CQRS.Interfaces;
public interface IEventDispatcher
{
    void Dispatch(IEnumerable<IDomainEvent> events);
    void Register<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent;
}