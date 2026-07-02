using Fiap.Workshop.Domain.Abstractions;

namespace Fiap.Workshop.Domain.Users.Events;

public sealed record UserCreatedEvent(Guid UserId) : IDomainEvent;
