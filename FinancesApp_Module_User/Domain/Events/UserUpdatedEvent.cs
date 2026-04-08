using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_User.Domain.Events;

public record UserUpdatedEvent(Guid EventId,
                               DateTimeOffset Timestamp,
                               Guid Id,
                               string Name,
                               string Email,
                               DateTimeOffset DateOfBirth,
                               string ProfileImage) : IDomainEvent;
