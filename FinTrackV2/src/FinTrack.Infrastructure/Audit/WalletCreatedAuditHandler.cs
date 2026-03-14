using System.Text.Json;
using FinTrack.Domain.Events;
using FinTrack.Infrastructure.Persistence;
using MediatR;

namespace FinTrack.Infrastructure.Audit;

/// <summary>
/// Listens for WalletCreatedEvent and writes an audit log entry.
/// The Wallet entity has no idea this handler exists — loose coupling.
/// </summary>
public sealed class WalletCreatedAuditHandler : INotificationHandler<WalletCreatedEvent>
{
    private readonly AppDbContext _db;

    public WalletCreatedAuditHandler(AppDbContext db) => _db = db;

    public async Task Handle(WalletCreatedEvent notification, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            notification.WalletId,
            notification.UserId,
            notification.Name,
            notification.Currency,
            notification.OccurredAt
        });

        var log = AuditLog.Create(nameof(WalletCreatedEvent), payload);
        await _db.AuditLogs.AddAsync(log, ct);
        await _db.SaveChangesAsync(ct);
    }
}
