using FinTrack.Application.Interfaces;

namespace FinTrack.Tests.Common;

/// <summary>
/// Captures sent emails for assertion — never actually sends anything.
/// </summary>
public sealed class FakeEmailService : IEmailService
{
    public record SentEmail(string To, string Subject, string Body);

    public List<SentEmail> SentEmails { get; } = [];

    public Task SendAsync(
        string toEmail, string subject, string body, CancellationToken ct = default)
    {
        SentEmails.Add(new SentEmail(toEmail, subject, body));
        return Task.CompletedTask;
    }
}
