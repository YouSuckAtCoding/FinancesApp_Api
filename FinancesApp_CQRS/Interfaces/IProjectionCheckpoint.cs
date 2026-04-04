namespace FinancesApp_CQRS.Interfaces;

public interface IProjectionCheckpoint
{
    /// <summary>
    /// Attempts to claim an event for projection processing.
    /// Returns true if the event was newly claimed and its handler should run.
    /// Returns false if the event was already applied and should be skipped.
    /// </summary>
    Task<bool> TryClaimAsync(Guid eventId, CancellationToken token = default);
}
