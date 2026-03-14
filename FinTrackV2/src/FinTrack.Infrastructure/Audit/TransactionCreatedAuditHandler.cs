using System.Text.Json;
using FinTrack.Domain.Events;
using FinTrack.Infrastructure.Persistence;
using MediatR;

namespace FinTrack.Infrastructure.Audit;

public sealed class TransactionCreatedAuditHandler : INotificationHandler<TransactionCreatedEvent>
{
    private readonly AppDbContext _db;

    public TransactionCreatedAuditHandler(AppDbContext db) => _db = db;

    public async Task Handle(TransactionCreatedEvent notification, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            notification.TransactionId,
            notification.WalletId,
            notification.UserId,
            notification.Type,
            notification.Amount,
            notification.BalanceAfter,
            notification.OccurredAt
        });

        var log = AuditLog.Create(nameof(TransactionCreatedEvent), payload);
        await _db.AuditLogs.AddAsync(log, ct);
        await _db.SaveChangesAsync(ct);
    }
}
