using FinTrack.WalletService.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WalletService.Data;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

    public DbSet<Wallet>            Wallets      => Set<Wallet>();
    public DbSet<WalletTransaction> Transactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wallet>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Name).HasMaxLength(100).IsRequired();
            e.Property(w => w.Currency).HasMaxLength(3).IsRequired();
            e.Property(w => w.Balance).HasColumnType("numeric(18,2)");
            e.HasIndex(w => w.UserId);
        });

        modelBuilder.Entity<WalletTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("numeric(18,2)");
            e.Property(t => t.BalanceBefore).HasColumnType("numeric(18,2)");
            e.Property(t => t.BalanceAfter).HasColumnType("numeric(18,2)");
            e.Property(t => t.Type).HasMaxLength(20).IsRequired();
            e.Property(t => t.Description).HasMaxLength(200);
            e.HasOne(t => t.Wallet)
             .WithMany(w => w.Transactions)
             .HasForeignKey(t => t.WalletId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
