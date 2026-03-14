using FinTrack.Application.Interfaces;
using FinTrack.Domain.Events;
using MediatR;

namespace FinTrack.Infrastructure.Events;

/// <summary>
/// Uses MediatR.Publish() to dispatch domain events to all registered handlers.
/// Adding a new handler (e.g. SendEmailHandler) requires zero changes here.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public DomainEventDispatcher(IPublisher publisher) => _publisher = publisher;

    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, ct);
    }
}
