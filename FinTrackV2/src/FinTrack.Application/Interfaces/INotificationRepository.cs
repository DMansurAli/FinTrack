using FinTrack.Domain.Entities;

namespace FinTrack.Application.Interfaces;

public interface INotificationRepository
{
    Task<List<NotificationMessage>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default);

    Task<NotificationMessage?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken ct = default);

    Task AddAsync(NotificationMessage notification, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
