using FinTrack.Domain.Entities;

namespace FinTrack.Application.Interfaces;

/// <summary>
/// Gives the OutboxProcessor access to unprocessed messages.
/// Defined in Application so Infrastructure implements it — not the other way around.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>Returns all rows where ProcessedAt IS NULL, oldest first.</summary>
    Task<List<OutboxMessage>> GetUnprocessedAsync(CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
