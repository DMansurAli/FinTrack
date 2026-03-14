using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WalletId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Wallet? Wallet { get; private set; }

    private Transaction() { }

    internal static Transaction Create(
        Guid walletId,
        TransactionType type,
        decimal amount,
        string description,
        decimal balanceBefore,
        decimal balanceAfter) =>
        new()
        {
            WalletId      = walletId,
            Type          = type,
            Amount        = amount,
            Description   = description.Trim(),
            BalanceBefore = balanceBefore,
            BalanceAfter  = balanceAfter,
        };
}
