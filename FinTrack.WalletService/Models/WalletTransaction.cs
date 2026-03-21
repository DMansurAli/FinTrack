namespace FinTrack.WalletService.Models;

public class WalletTransaction
{
    public Guid     Id            { get; set; } = Guid.NewGuid();
    public Guid     WalletId      { get; set; }
    public string   Type          { get; set; } = string.Empty; // "Deposit" / "Withdrawal"
    public decimal  Amount        { get; set; }
    public decimal  BalanceBefore { get; set; }
    public decimal  BalanceAfter  { get; set; }
    public string   Description   { get; set; } = string.Empty;
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;

    public Wallet Wallet { get; set; } = null!;
}
