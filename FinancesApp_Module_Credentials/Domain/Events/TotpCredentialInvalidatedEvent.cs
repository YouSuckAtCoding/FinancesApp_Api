using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Credentials.Domain.Events;
public record TotpCredentialInvalidatedEvent(Guid EventId,
                                             DateTimeOffset Timestamp,
                                             Guid TotpId,
                                             Guid UserId) : IDomainEvent
{
}
