using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Mapper;
using Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItem.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItem;

public sealed class GetInventoryItemUseCase(
    IInventoryItemRepository inventoryItemRepository,
    ILogger<GetInventoryItemUseCase> logger
) : IGetInventoryItemUseCase
{
    private readonly IInventoryItemRepository _inventoryItemRepository = inventoryItemRepository;
    private readonly ILogger<GetInventoryItemUseCase> _logger = logger;

    public async Task<Output> Handle(GetInventoryItemInput input, CancellationToken cancellationToken)
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

        output.AddResult(inventoryItem.MapToDto());

        return output;
    }
}
