namespace Fiap.Workshop.Domain.Abstractions;

public abstract class AggregateRoot<TId> : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) => Id = id;

    protected AggregateRoot() { }

    public TId Id { get; protected set; } = default!;

    public IReadOnlyList<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
