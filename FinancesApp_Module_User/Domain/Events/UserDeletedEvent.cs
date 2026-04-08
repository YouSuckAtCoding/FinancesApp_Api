using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_User.Domain.Events;

public record UserDeletedEvent(Guid EventId,
                               DateTimeOffset Timestamp,
                               Guid Id) : IDomainEvent;
