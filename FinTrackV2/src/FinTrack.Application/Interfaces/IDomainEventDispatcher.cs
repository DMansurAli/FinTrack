using FinTrack.Domain.Events;

namespace FinTrack.Application.Interfaces;

/// <summary>
/// Dispatches domain events after the main operation completes.
/// Infrastructure implements this using MediatR.Publish().
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken ct = default);
}
