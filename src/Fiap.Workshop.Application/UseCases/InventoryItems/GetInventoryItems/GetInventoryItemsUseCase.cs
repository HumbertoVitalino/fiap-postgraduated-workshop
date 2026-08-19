using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItems.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItems;

public sealed class GetInventoryItemsUseCase(
    IInventoryItemRepository inventoryItemRepository,
    ILogger<GetInventoryItemsUseCase> logger
) : IGetInventoryItemsUseCase
{
    private readonly IInventoryItemRepository _inventoryItemRepository = inventoryItemRepository;
    private readonly ILogger<GetInventoryItemsUseCase> _logger = logger;

    public async Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken)
    {
        Output output = new();

        var inventoryItems = await _inventoryItemRepository.GetAllAsync(cancellationToken);
        if (!inventoryItems.Any())
        {
            _logger.LogWarning("[{CorrelationId}] | Unable to find inventory items", correlationId);

            output.AddResult(Array.Empty<InventoryItemResponse>());
            return output;
        }

        output.AddResult(inventoryItems.MapToDto());
        return output;
    }
}
