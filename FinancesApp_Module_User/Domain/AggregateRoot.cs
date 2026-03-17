using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_User.Domain;
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];
    public int Version { get; private set; }

    protected void Raise(IDomainEvent evt)
    {
        Apply(evt);                   
        _uncommittedEvents.Add(evt);  
        Version++;
    }

    protected abstract void Apply(IDomainEvent evt);

    public IReadOnlyList<IDomainEvent> GetUncommittedEvents() => _uncommittedEvents;
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
}
