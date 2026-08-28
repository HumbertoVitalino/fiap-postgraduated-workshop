using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem;

public sealed class CreateInventoryItemUseCase(
    IInventoryItemRepository inventoryItemRepository,
    ILogger<CreateInventoryItemUseCase> logger
) : ICreateInventoryItemUseCase
{
    private readonly IInventoryItemRepository _inventoryItemRepository = inventoryItemRepository;
    private readonly ILogger<CreateInventoryItemUseCase> _logger = logger;

    public async Task<Output> Handle(CreateInventoryItemInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var codeInUse = await _inventoryItemRepository.ExistsWithCodeAsync(input.Code, cancellationToken);
        if (codeInUse)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Inventory item with code {Code} already exists.",
                input.CorrelationId,
                input.Code.SanitizeForLog()
            );

            output.AddErrorMessage($"Inventory item with code {input.Code} already exists.");
            return output;
        }

        var inventoryItem = input.MapToDomain();

        await _inventoryItemRepository.AddAsync(inventoryItem, cancellationToken);

        var saved = await _inventoryItemRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error saving inventory item with code {Code}.",
                input.CorrelationId,
                input.Code.SanitizeForLog()
            );

            output.AddErrorMessage($"Error saving inventory item with code {input.Code}.");
            return output;
        }

        output.AddResult(inventoryItem.MapToDto());
        return output;
    }
}
