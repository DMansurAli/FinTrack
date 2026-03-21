using FinTrack.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinTrack.Infrastructure.Email;

/// <summary>
/// Development stub — logs email to console instead of sending.
/// Replace with SendGridEmailService or SmtpEmailService in production
/// by registering a different IEmailService implementation.
/// Zero handler changes required.
/// </summary>
public sealed class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
        => _logger = logger;

    public Task SendAsync(
        string toEmail, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] To: {To} | Subject: {Subject}\n{Body}",
            toEmail, subject, body);

        return Task.CompletedTask;
    }
}
