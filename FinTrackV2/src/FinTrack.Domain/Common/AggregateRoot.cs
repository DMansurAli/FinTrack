using FinTrack.Domain.Events;

namespace FinTrack.Domain.Common;

/// <summary>
/// An aggregate root is an entity that owns a cluster of related objects
/// and is the only entry point for changes to that cluster.
/// It also collects domain events that happened during the operation.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
