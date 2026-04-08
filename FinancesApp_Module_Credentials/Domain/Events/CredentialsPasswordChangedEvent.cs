using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Credentials.Domain.Events;

public record CredentialsPasswordChangedEvent(Guid EventId,
                                              DateTimeOffset Timestamp,
                                              Guid Id,
                                              Guid UserId,
                                              string NewPasswordHash) : IDomainEvent;
