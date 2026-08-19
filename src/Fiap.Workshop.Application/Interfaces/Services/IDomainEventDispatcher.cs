using Fiap.Workshop.Domain.Abstractions;

namespace Fiap.Workshop.Application.Interfaces.Services;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
