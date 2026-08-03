namespace Fiap.Workshop.Domain.Abstractions;

public abstract class AggregateRoot(
    Guid id,
    DateTime createdAt,
    DateTime updatedAt
)
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = id;
    public DateTime CreatedAt { get; protected set; } = createdAt;
    public DateTime UpdatedAt { get; protected set; } = updatedAt;
    public IReadOnlyList<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    protected void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;
}
