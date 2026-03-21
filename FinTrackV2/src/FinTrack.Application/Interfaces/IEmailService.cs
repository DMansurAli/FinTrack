namespace FinTrack.Application.Interfaces;

/// <summary>
/// Abstraction over any email provider (SendGrid, Mailgun, SMTP).
/// Infrastructure provides ConsoleEmailService in dev.
/// Swap to a real provider by registering a different implementation — zero handler changes.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}
