using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Credentials.Domain.Events;
public record TotpCredentialCreatedEvent(Guid EventId,
                                         DateTimeOffset Timestamp,
                                         Guid TotpId,
                                         Guid UserId,
                                         string SecurityCode) : IDomainEvent
{
}
