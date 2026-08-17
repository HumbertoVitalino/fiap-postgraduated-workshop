using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.InventoryItems.UpdateInventoryItem.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IUpdateInventoryItemUseCase
{
    Task<Output> Handle(UpdateInventoryItemInput input, CancellationToken cancellationToken);
}
