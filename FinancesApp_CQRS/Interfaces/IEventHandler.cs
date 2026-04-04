namespace FinancesApp_CQRS.Interfaces;
public interface IEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent evt, CancellationToken token = default);
}