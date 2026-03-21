using FinTrack.NotifyService.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.NotifyService.Data;

public class NotifyDbContext : DbContext
{
    public NotifyDbContext(DbContextOptions<NotifyDbContext> options) : base(options) { }

    public DbSet<NotificationItem> Notifications => Set<NotificationItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationItem>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.Property(n => n.Body).HasMaxLength(1000).IsRequired();
            e.HasIndex(n => n.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
