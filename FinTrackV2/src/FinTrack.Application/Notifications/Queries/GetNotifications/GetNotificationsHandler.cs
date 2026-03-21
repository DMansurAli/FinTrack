using FinTrack.Application.Interfaces;
using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsHandler
    : IRequestHandler<GetNotificationsQuery, Result<List<NotificationResponse>>>
{
    private readonly INotificationRepository _notifications;

    public GetNotificationsHandler(INotificationRepository notifications)
        => _notifications = notifications;

    public async Task<Result<List<NotificationResponse>>> Handle(
        GetNotificationsQuery query, CancellationToken ct)
    {
        var items = await _notifications.GetByUserIdAsync(query.UserId, ct);

        var response = items
            .OrderByDescending(n => n.CreatedAt)
            .Select(ToResponse)
            .ToList();

        return Result.Success(response);
    }

    internal static NotificationResponse ToResponse(
        FinTrack.Domain.Entities.NotificationMessage n) =>
        new(n.Id, n.Title, n.Body, n.IsRead, n.CreatedAt, n.ReadAt);
}
