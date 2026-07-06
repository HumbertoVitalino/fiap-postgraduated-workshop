using Fiap.Workshop.Domain.Abstractions;

namespace Fiap.Workshop.Application.Abstractions;

public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
