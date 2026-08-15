using Fiap.Workshop.Application.Commons;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetInventoryItemsUseCase
{
    Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken);
}
