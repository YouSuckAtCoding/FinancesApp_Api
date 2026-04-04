namespace FinancesApp_CQRS.Interfaces;
public interface IEventDispatcher
{
    Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken token = default);
    Task Dispatch(IDomainEvent evt, CancellationToken token = default);
    void Register<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent;
}