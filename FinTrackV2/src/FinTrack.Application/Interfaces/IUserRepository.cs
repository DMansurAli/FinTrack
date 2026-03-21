using FinTrack.Domain.Entities;

namespace FinTrack.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool>  ExistsWithEmailAsync(string email, CancellationToken ct = default);
    Task        AddAsync(User user, CancellationToken ct = default);
    Task        SaveChangesAsync(CancellationToken ct = default);
}
