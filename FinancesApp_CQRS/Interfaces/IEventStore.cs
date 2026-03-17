namespace FinancesApp_CQRS.Interfaces;
public interface IEventStore
{
    void Dispose();
    Task Append(Guid aggregateId,
                IReadOnlyList<IDomainEvent> events,
                int expectedVersion,
                CancellationToken token = default);
    Task<List<IDomainEvent>> Load(Guid aggregateId, int fromVersion = 0, CancellationToken token = default);
    Task<List<IDomainEvent>> LoadAuditLog(DateTimeOffset from, DateTimeOffset to, CancellationToken token = default);
    Task<List<IDomainEvent>> LoadByDateRange(Guid aggregateId, DateTimeOffset from, DateTimeOffset to, CancellationToken token = default);
}