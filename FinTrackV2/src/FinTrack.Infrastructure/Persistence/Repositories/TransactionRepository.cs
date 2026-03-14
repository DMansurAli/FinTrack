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

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default) =>
        await _db.Transactions.AddAsync(transaction, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
