using FinTrack.Domain.Common;
using FinTrack.Domain.Errors;

namespace FinTrack.Domain.Entities;

public class Wallet
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public decimal Balance { get; private set; } = 0;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public User? User { get; private set; }

    private Wallet() { }

    public static Wallet Create(Guid userId, string name, string currency) =>
        new()
        {
            UserId   = userId,
            Name     = name.Trim(),
            Currency = currency.ToUpperInvariant(),
        };

    /// <summary>
    /// Business rule: renaming lives on the entity, not in a service or controller.
    /// Returns a Result so callers know if it failed and why.
    /// </summary>
    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(WalletErrors.InvalidName);

        Name      = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
