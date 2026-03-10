using FinTrack.Application.Interfaces;
using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class WalletRepository : IWalletRepository
{
    private readonly AppDbContext _db;

    public WalletRepository(AppDbContext db) => _db = db;

    public Task<List<Wallet>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.Wallets
           .Where(w => w.UserId == userId)
           .OrderByDescending(w => w.CreatedAt)
           .ToListAsync(ct);

    public Task<Wallet?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        _db.Wallets.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

    public async Task AddAsync(Wallet wallet, CancellationToken ct = default) =>
        await _db.Wallets.AddAsync(wallet, ct);

    public Task RemoveAsync(Wallet wallet, CancellationToken ct = default)
    {
        _db.Wallets.Remove(wallet);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
