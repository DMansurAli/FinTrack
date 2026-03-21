using FinTrack.Application.Interfaces;
using FinTrack.Domain.Entities;

namespace FinTrack.Tests.Common;

/// <summary>
/// In-memory notification store for unit tests.
/// No EF Core, no database — just a List.
/// </summary>
public sealed class FakeNotificationRepository : INotificationRepository
{
    private readonly List<NotificationMessage> _store = [];

    public Task<List<NotificationMessage>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default) =>
        Task.FromResult(_store.Where(n => n.UserId == userId)
                              .OrderByDescending(n => n.CreatedAt)
                              .ToList());

    public Task<NotificationMessage?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(n => n.Id == id && n.UserId == userId));

    public Task AddAsync(NotificationMessage notification, CancellationToken ct = default)
    {
        _store.Add(notification);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        Task.CompletedTask;
}
