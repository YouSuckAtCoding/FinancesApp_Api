using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Credentials.Domain.Events;

public record CredentialsDeletedEvent(Guid EventId,
                                      DateTimeOffset Timestamp,
                                      Guid Id,
                                      Guid UserId) : IDomainEvent;
