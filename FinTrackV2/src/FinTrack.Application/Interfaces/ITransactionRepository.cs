using FinTrack.Domain.Entities;

namespace FinTrack.Application.Interfaces;

public interface ITransactionRepository
{
    Task<List<Transaction>> GetByWalletIdAsync(Guid walletId, CancellationToken ct = default);
    Task<(List<Transaction> Items, int TotalCount)> GetPagedByWalletIdAsync(
        Guid walletId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
