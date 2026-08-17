using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItem.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetInventoryItemUseCase
{
    Task<Output> Handle(GetInventoryItemInput input, CancellationToken cancellationToken);
}
