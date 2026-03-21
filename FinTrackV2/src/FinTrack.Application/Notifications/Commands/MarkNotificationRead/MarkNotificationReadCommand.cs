using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId)
    : IRequest<Result>;
