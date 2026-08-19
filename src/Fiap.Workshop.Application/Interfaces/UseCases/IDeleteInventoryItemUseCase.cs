using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.InventoryItems.DeleteInventoryItem.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IDeleteInventoryItemUseCase
{
    Task<Output> Handle(DeleteInventoryItemInput input, CancellationToken cancellationToken);
}
