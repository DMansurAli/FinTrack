using FinTrack.Application.Interfaces;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Events;
using MediatR;

namespace FinTrack.Application.Notifications.EventHandlers;

public sealed class TransactionNotificationHandler
    : INotificationHandler<TransactionCreatedEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository         _users;
    private readonly IEmailService           _email;

    public TransactionNotificationHandler(
        INotificationRepository notifications,
        IUserRepository users,
        IEmailService email)
    {
        _notifications = notifications;
        _users         = users;
        _email         = email;
    }

    public async Task Handle(TransactionCreatedEvent e, CancellationToken ct)
    {
        var typeLabel = e.Type.ToString();  // "Deposit" or "Withdrawal"
        var title     = $"{typeLabel} received";
        var body      = $"{typeLabel} of {e.Amount:N2} — new balance: {e.BalanceAfter:N2}";

        // ── In-app notification ───────────────────────────────────────────
        var notification = NotificationMessage.Create(e.UserId, title, body);
        await _notifications.AddAsync(notification, ct);
        await _notifications.SaveChangesAsync(ct);

        // ── Email (console stub in dev, replace with SendGrid in prod) ────
        var user = await _users.GetByIdAsync(e.UserId, ct);
        if (user is not null)
            await _email.SendAsync(
                user.Email,
                title,
                $"Hi {user.FirstName},\n\n{body}\n\nFinTrack",
                ct);
    }
}
