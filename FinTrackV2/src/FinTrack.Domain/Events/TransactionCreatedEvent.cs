using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Events;

public record TransactionCreatedEvent(
    Guid TransactionId,
    Guid WalletId,
    Guid UserId,
    TransactionType Type,
    decimal Amount,
    decimal BalanceAfter) : DomainEvent;
