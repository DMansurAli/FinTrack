using FinTrack.Application.Interfaces;
using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _db;

    public OutboxRepository(AppDbContext db) => _db = db;

    public Task<List<OutboxMessage>> GetUnprocessedAsync(CancellationToken ct = default) =>
        _db.OutboxMessages
           .Where(m => m.ProcessedAt == null)
           .OrderBy(m => m.OccurredAt)
           .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
