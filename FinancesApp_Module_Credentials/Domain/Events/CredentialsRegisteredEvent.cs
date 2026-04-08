using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Credentials.Domain.Events;

public record CredentialsRegisteredEvent(Guid EventId,
                                         DateTimeOffset Timestamp,
                                         Guid Id,
                                         Guid UserId,
                                         string Email,
                                         string PasswordHash) : IDomainEvent;
