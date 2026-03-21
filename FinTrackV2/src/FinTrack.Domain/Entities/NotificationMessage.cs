namespace FinTrack.Domain.Entities;

/// <summary>
/// An in-app notification shown to the user.
/// Created by NotificationHandler when a domain event fires.
/// Users can list and mark notifications as read via API.
/// </summary>
public class NotificationMessage
{
    public Guid     Id        { get; private set; } = Guid.NewGuid();
    public Guid     UserId    { get; private set; }

    /// <summary>Short human-readable title, e.g. "Deposit received"</summary>
    public string   Title     { get; private set; } = string.Empty;

    /// <summary>Full message body, e.g. "USD 500.00 deposited into Savings"</summary>
    public string   Body      { get; private set; } = string.Empty;

    /// <summary>Null = unread.</summary>
    public DateTime? ReadAt   { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public bool IsRead => ReadAt.HasValue;

    private NotificationMessage() { }

    public static NotificationMessage Create(Guid userId, string title, string body) =>
        new() { UserId = userId, Title = title, Body = body };

    public void MarkRead()
    {
        if (!IsRead)
            ReadAt = DateTime.UtcNow;
    }
}
