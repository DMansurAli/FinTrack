namespace FinTrack.Domain.Entities;

/// <summary>
/// Persisted record of a domain event that needs to be dispatched.
/// Written in the SAME database transaction as the business data it describes.
/// A BackgroundService reads unprocessed rows and dispatches them via MediatR.
///
/// Why this exists:
///   Without the Outbox, if the process crashes between SaveChanges() and
///   DomainEventDispatcher.Dispatch(), the event is silently lost.
///   With the Outbox, the event row survives and is retried on next poll.
/// </summary>
public class OutboxMessage
{
    public Guid      Id          { get; private set; } = Guid.NewGuid();

    /// <summary>Full type name, e.g. "TransactionCreatedEvent"</summary>
    public string    Type        { get; private set; } = string.Empty;

    /// <summary>JSON-serialised domain event payload</summary>
    public string    Payload     { get; private set; } = string.Empty;

    public DateTime  OccurredAt  { get; private set; } = DateTime.UtcNow;

    /// <summary>Null = not yet processed. Set by OutboxProcessor after dispatch.</summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>Stores the exception message if dispatch failed.</summary>
    public string?   Error       { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string type, string payload) =>
        new() { Type = type, Payload = payload };

    public void MarkProcessed() =>
        ProcessedAt = DateTime.UtcNow;

    public void MarkFailed(string error)
    {
        Error       = error;
        ProcessedAt = DateTime.UtcNow; // prevents infinite retry loops
    }
}
