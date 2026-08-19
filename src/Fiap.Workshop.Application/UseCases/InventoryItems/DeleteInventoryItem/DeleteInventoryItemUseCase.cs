using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.InventoryItems.DeleteInventoryItem.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.InventoryItems.DeleteInventoryItem;

public sealed class DeleteInventoryItemUseCase(
    IInventoryItemRepository inventoryItemRepository,
    IServiceOrderRepository serviceOrderRepository,
    ILogger<DeleteInventoryItemUseCase> logger
) : IDeleteInventoryItemUseCase
{
    private readonly IInventoryItemRepository _inventoryItemRepository = inventoryItemRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly ILogger<DeleteInventoryItemUseCase> _logger = logger;

    public async Task<Output> Handle(DeleteInventoryItemInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var inventoryItem = await _inventoryItemRepository.GetByIdAsync(input.InventoryItemId, cancellationToken);
        if (inventoryItem is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Unable to find inventory item by [{InventoryItemId}].",
                input.CorrelationId,
                input.InventoryItemId
            );

            return output;
        }

        var hasBeenUsed = await _serviceOrderRepository.ExistsWithInventoryItemIdAsync(inventoryItem.Id, cancellationToken);
        if (hasBeenUsed)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Inventory item with id {InventoryItemId} has been used in a service order and cannot be deleted.",
                input.CorrelationId,
                input.InventoryItemId
            );

            output.AddErrorMessage("Inventory item has been used in a service order and cannot be deleted.");
            return output;
        }

        _inventoryItemRepository.Remove(inventoryItem);

        var saved = await _inventoryItemRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error deleting inventory item with id {InventoryItemId}.",
                input.CorrelationId,
                input.InventoryItemId
            );

            output.AddErrorMessage($"Error deleting inventory item with id {input.InventoryItemId}.");
            return output;
        }

        return output;
    }
}
