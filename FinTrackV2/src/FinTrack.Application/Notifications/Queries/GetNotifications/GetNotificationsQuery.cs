using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(Guid UserId)
    : IRequest<Result<List<NotificationResponse>>>;
