namespace FinancesApp_CQRS.Outbox;

public record OutboxEntry(long Id, string EventType, string Payload, int RetryCount);