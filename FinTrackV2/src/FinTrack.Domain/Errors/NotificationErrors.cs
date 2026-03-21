using FinTrack.Domain.Common;

namespace FinTrack.Domain.Errors;

public static class NotificationErrors
{
    public static readonly Error NotFound = new(
        "Notification.NotFound",
        "The notification was not found or does not belong to you.");
}
