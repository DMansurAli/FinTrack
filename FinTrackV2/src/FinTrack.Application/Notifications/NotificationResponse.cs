namespace FinTrack.Application.Notifications;

/// <summary>DTO returned by all notification endpoints.</summary>
public record NotificationResponse(
    Guid     Id,
    string   Title,
    string   Body,
    bool     IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);
