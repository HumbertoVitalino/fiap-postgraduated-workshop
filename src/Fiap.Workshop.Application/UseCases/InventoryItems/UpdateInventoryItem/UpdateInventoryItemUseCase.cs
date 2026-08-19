using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Mapper;
using Fiap.Workshop.Application.UseCases.InventoryItems.UpdateInventoryItem.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.InventoryItems.UpdateInventoryItem;

public sealed class UpdateInventoryItemUseCase(
    IInventoryItemRepository inventoryItemRepository,
    ILogger<UpdateInventoryItemUseCase> logger
) : IUpdateInventoryItemUseCase
{
    private readonly IInventoryItemRepository _inventoryItemRepository = inventoryItemRepository;
    private readonly ILogger<UpdateInventoryItemUseCase> _logger = logger;

    public async Task<Output> Handle(UpdateInventoryItemInput input, CancellationToken cancellationToken)
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

            output.AddErrorMessage("Unable to find inventory item");
            return output;
        }

        inventoryItem.UpdateProfile(input.Name, input.Description, input.UnitPrice, input.MinimumStock, input.UnitOfMeasure, input.IsActive);

        _inventoryItemRepository.Update(inventoryItem);

        var saved = await _inventoryItemRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error updating inventory item with id {InventoryItemId}.",
                input.CorrelationId,
                input.InventoryItemId
            );

            output.AddErrorMessage($"Error updating inventory item with id {input.InventoryItemId}.");
            return output;
        }

        output.AddResult(inventoryItem.MapToDto());
        return output;
    }
}
