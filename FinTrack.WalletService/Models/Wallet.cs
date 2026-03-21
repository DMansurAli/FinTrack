namespace FinTrack.WalletService.Models;

public class Wallet
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public Guid     UserId    { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public string   Currency  { get; set; } = string.Empty;
    public decimal  Balance   { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WalletTransaction> Transactions { get; set; } = [];
}
