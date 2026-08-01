using Fiap.Workshop.Domain.Abstractions;

namespace Fiap.Workshop.Domain.Events;

public sealed record UserCreatedEvent(Guid UserId) : IDomainEvent;
