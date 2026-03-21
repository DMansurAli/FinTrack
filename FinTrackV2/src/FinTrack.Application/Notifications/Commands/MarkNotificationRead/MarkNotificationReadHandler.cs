using FinTrack.Application.Interfaces;
using FinTrack.Domain.Common;
using FinTrack.Domain.Errors;
using MediatR;

namespace FinTrack.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadHandler
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly INotificationRepository _notifications;

    public MarkNotificationReadHandler(INotificationRepository notifications)
        => _notifications = notifications;

    public async Task<Result> Handle(
        MarkNotificationReadCommand command, CancellationToken ct)
    {
        var notification = await _notifications
            .GetByIdAndUserIdAsync(command.NotificationId, command.UserId, ct);

        if (notification is null)
            return Result.Failure(NotificationErrors.NotFound);

        notification.MarkRead();
        await _notifications.SaveChangesAsync(ct);

        return Result.Success();
    }
}
