namespace FinTrack.Infrastructure.Audit;

/// <summary>
/// Persisted record of every significant domain event.
/// Written by event handlers, never by business logic directly.
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;

    private AuditLog() { }

    public static AuditLog Create(string eventType, string payload) =>
        new() { EventType = eventType, Payload = payload };
}
