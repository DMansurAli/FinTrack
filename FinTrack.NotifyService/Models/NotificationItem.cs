namespace FinTrack.NotifyService.Models;

public class NotificationItem
{
    public Guid      Id        { get; set; } = Guid.NewGuid();
    public Guid      UserId    { get; set; }
    public string    Title     { get; set; } = string.Empty;
    public string    Body      { get; set; } = string.Empty;
    public DateTime? ReadAt    { get; set; }
    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead => ReadAt.HasValue;
}
