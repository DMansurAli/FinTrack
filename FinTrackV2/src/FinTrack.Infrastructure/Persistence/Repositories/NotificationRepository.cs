using FinTrack.Application.Interfaces;
using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db) => _db = db;

    public Task<List<NotificationMessage>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default) =>
        _db.Notifications
           .Where(n => n.UserId == userId)
           .OrderByDescending(n => n.CreatedAt)
           .ToListAsync(ct);

    public Task<NotificationMessage?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken ct = default) =>
        _db.Notifications
           .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

    public async Task AddAsync(NotificationMessage notification, CancellationToken ct = default) =>
        await _db.Notifications.AddAsync(notification, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
