namespace FinancesApp_CQRS.Interfaces;
public interface IEventHandler<TEvent> where TEvent : IDomainEvent
{
    void Handle(TEvent evt);
}