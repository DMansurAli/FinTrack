namespace FinTrack.Contracts.Messages;

/// <summary>
/// Published by WalletService to RabbitMQ when a transaction is created.
/// Consumed by NotifyService to create notifications and send emails.
/// Both services reference this type from FinTrack.Contracts so they
/// can never get out of sync on the message shape.
/// </summary>
public record TransactionCreatedMessage
{
    public Guid     TransactionId { get; init; }
    public Guid     WalletId      { get; init; }
    public Guid     UserId        { get; init; }
    public string   UserEmail     { get; init; } = string.Empty;
    public string   UserFirstName { get; init; } = string.Empty;
    public string   Type          { get; init; } = string.Empty;
    public decimal  Amount        { get; init; }
    public decimal  BalanceAfter  { get; init; }
    public DateTime OccurredAt    { get; init; }
}
