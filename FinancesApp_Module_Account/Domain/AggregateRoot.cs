using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Account.Domain;
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];
    public int NextVersion { get; private set; }
    public int CurrentVersion { get; private set; }

    protected void Raise(IDomainEvent evt)
    {
        Apply(evt);
        _uncommittedEvents.Add(evt);
        NextVersion++;
    }
    
    public void SetAggregateVersions(int current)
    {
        CurrentVersion = current;
        NextVersion += current;
    }

    protected abstract void Apply(IDomainEvent evt);
    public abstract void RebuildFromEvents(List<IDomainEvent> events);
    public IReadOnlyList<IDomainEvent> GetUncommittedEvents() => _uncommittedEvents;
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
}
