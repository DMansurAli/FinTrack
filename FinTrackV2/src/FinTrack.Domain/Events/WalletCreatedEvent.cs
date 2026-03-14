namespace FinTrack.Domain.Events;

public record WalletCreatedEvent(Guid WalletId, Guid UserId, string Name, string Currency)
    : DomainEvent;
