using FinTrack.Domain.Entities;

namespace FinTrack.Application.Interfaces;

public interface IWalletRepository
{
    Task<List<Wallet>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Wallet?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddAsync(Wallet wallet, CancellationToken ct = default);
    Task RemoveAsync(Wallet wallet, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
