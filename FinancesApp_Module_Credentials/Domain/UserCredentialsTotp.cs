using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Domain.Events;

namespace FinancesApp_Module_Credentials.Domain;

public class UserCredentialsTotp : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset InvalidAt { get; private set; }
    public string SecurityCode { get; private set; } = string.Empty;
    public bool Active { get; private set; }

    public UserCredentialsTotp()
    {

    }

    // Read-model constructor used by projection read repository
    public UserCredentialsTotp(Guid id, Guid userId, string securityCode,
                               DateTimeOffset createdAt, DateTimeOffset invalidAt, bool active)
    {
        Id = id;
        UserId = userId;
        SecurityCode = securityCode;
        CreatedAt = createdAt;
        InvalidAt = invalidAt;
        Active = active;
    }

    public UserCredentialsTotp(Guid userId, string securityCode)
    {
        Raise(new TotpCredentialCreatedEvent(EventId: Guid.NewGuid(),
                                             Timestamp: DateTimeOffset.UtcNow,
                                             TotpId: Guid.NewGuid(),
                                             UserId: userId,
                                             SecurityCode: securityCode));

    }
    public void Invalidate()
    {
        if (!Active)
            throw new InvalidOperationException("Credential is already invalidated.");
        Raise(new TotpCredentialInvalidatedEvent(EventId: Guid.NewGuid(),
                                                 Timestamp: DateTimeOffset.UtcNow,
                                                 TotpId: Id,
                                                 UserId: UserId));
    }

    public override void RebuildFromEvents(List<IDomainEvent> events)
    {
        ClearUncommittedEvents();
        foreach (var evt in events)
            Apply(evt);
        SetAggregateVersions(events.Count);
    }

    protected override void Apply(IDomainEvent evt)
    {
        switch (evt)
        {
            case TotpCredentialCreatedEvent e:
                Id = e.TotpId;
                UserId = e.UserId;
                SecurityCode = e.SecurityCode;
                CreatedAt = e.Timestamp;
                InvalidAt = CreatedAt.AddMinutes(5);
                Active = true;
                break;
            case TotpCredentialInvalidatedEvent:
                Active = false;
                break;
            default:
                throw new InvalidOperationException($"Unknown event type: {evt.GetType().Name}");
        }
    }
}
