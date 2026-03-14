using FinTrack.Application.Interfaces;
using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _db;

    public TransactionRepository(AppDbContext db) => _db = db;

    public Task<List<Transaction>> GetByWalletIdAsync(Guid walletId, CancellationToken ct = default) =>
        _db.Transactions
           .Where(t => t.WalletId == walletId)
           .OrderByDescending(t => t.CreatedAt)
           .ToListAsync(ct);

    public async Task<(List<Transaction> Items, int TotalCount)> GetPagedByWalletIdAsync(
        Guid walletId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Transactions
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items      = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default) =>
        await _db.Transactions.AddAsync(transaction, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
