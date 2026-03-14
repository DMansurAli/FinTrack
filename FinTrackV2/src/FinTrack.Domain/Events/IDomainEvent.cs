using MediatR;

namespace FinTrack.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Extends INotification so MediatR can dispatch them.
/// Domain events are things that HAPPENED — past tense, immutable.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
