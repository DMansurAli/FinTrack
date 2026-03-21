using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User>                Users         => Set<User>();
    public DbSet<Wallet>              Wallets        => Set<Wallet>();
    public DbSet<Transaction>         Transactions   => Set<Transaction>();
    public DbSet<AuditLog>            AuditLogs      => Set<AuditLog>();
    public DbSet<OutboxMessage>       OutboxMessages => Set<OutboxMessage>();
    public DbSet<NotificationMessage> Notifications  => Set<NotificationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Wallet>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Name).HasMaxLength(100).IsRequired();
            e.Property(w => w.Currency).HasMaxLength(3).IsRequired();
            e.Property(w => w.Balance).HasColumnType("numeric(18,2)");
            e.HasOne(w => w.User)
             .WithMany(u => u.Wallets)
             .HasForeignKey(w => w.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("numeric(18,2)");
            e.Property(t => t.BalanceBefore).HasColumnType("numeric(18,2)");
            e.Property(t => t.BalanceAfter).HasColumnType("numeric(18,2)");
            e.Property(t => t.Description).HasMaxLength(200);
            e.Property(t => t.Type).HasConversion<int>();
            e.HasOne(t => t.Wallet)
             .WithMany(w => w.Transactions)
             .HasForeignKey(t => t.WalletId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.EventType).HasMaxLength(100).IsRequired();
            e.Property(a => a.Payload).IsRequired();
        });

        // ── Step 5: Outbox ────────────────────────────────────────────────
        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Type).HasMaxLength(200).IsRequired();
            e.Property(o => o.Payload).IsRequired();
            // Index on ProcessedAt — the processor queries WHERE ProcessedAt IS NULL
            e.HasIndex(o => o.ProcessedAt);
        });

        // ── Step 5: Notifications ─────────────────────────────────────────
        modelBuilder.Entity<NotificationMessage>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.Property(n => n.Body).HasMaxLength(1000).IsRequired();
            // Index on UserId — every query filters by user
            e.HasIndex(n => n.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
