using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ICreateInventoryItemUseCase
{
    Task<Output> Handle(CreateInventoryItemInput input, CancellationToken cancellationToken);
}
