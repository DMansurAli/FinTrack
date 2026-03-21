using FinTrack.Contracts.Messages;
using FinTrack.NotifyService.Data;
using FinTrack.NotifyService.Models;
using MassTransit;

namespace FinTrack.NotifyService.Consumers;

/// <summary>
/// Consumes TransactionCreatedMessage from RabbitMQ.
///
/// MassTransit automatically:
///   - Creates the queue "fintrack.contracts.messages:transaction-created-message"
///   - Binds it to the exchange
///   - Calls Consume() for each message
///   - Acknowledges (ACKs) the message after Consume() returns successfully
///   - Dead-letters the message if Consume() throws (with retry policy)
///
/// NotifyService has ZERO knowledge of WalletService.
/// Adding this consumer required ZERO changes to WalletService.
/// </summary>
public class TransactionCreatedConsumer : IConsumer<TransactionCreatedMessage>
{
    private readonly NotifyDbContext _db;
    private readonly ILogger<TransactionCreatedConsumer> _logger;

    public TransactionCreatedConsumer(NotifyDbContext db,
                                      ILogger<TransactionCreatedConsumer> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionCreatedMessage> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Received TransactionCreatedMessage: {Type} {Amount} for user {UserId}",
            msg.Type, msg.Amount, msg.UserId);

        var title = $"{msg.Type} received";
        var body  = $"{msg.Type} of {msg.Amount:N2} — new balance: {msg.BalanceAfter:N2}";

        var notification = new NotificationItem
        {
            UserId = msg.UserId,
            Title  = title,
            Body   = body,
        };

        await _db.Notifications.AddAsync(notification, context.CancellationToken);
        await _db.SaveChangesAsync(context.CancellationToken);

        // ── Email stub ────────────────────────────────────────────────────
        // In production: inject IEmailService (SendGrid, Mailgun, SMTP)
        _logger.LogInformation(
            "[EMAIL STUB] To: {Email} | Subject: {Title} | Hi {Name}, {Body}",
            msg.UserEmail, title, msg.UserFirstName, body);
    }
}
