using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Mapper;

public static class CreateInventoryItemMapper
{
    private const int DEFAULT_RESERVED_QUANTITY = 0;

    public static InventoryItem MapToDomain(this CreateInventoryItemInput input)
    {
        return new(
            Guid.NewGuid(),
            input.Code,
            input.Name,
            input.Description,
            input.QuantityOnHand,
            DEFAULT_RESERVED_QUANTITY,
            input.MinimumStock,
            input.UnitPrice,
            input.UnitOfMeasure,
            true,
            DateTime.Now,
            DateTime.Now
        );
    }

    public static InventoryItemResponse MapToDto(this InventoryItem inventoryItem)
    {
        return new(
            inventoryItem.Id,
            inventoryItem.Code,
            inventoryItem.Name,
            inventoryItem.Description,
            inventoryItem.QuantityOnHand,
            inventoryItem.ReservedQuantity,
            inventoryItem.MinimumStock,
            inventoryItem.UnitPrice,
            inventoryItem.UnitOfMeasure,
            inventoryItem.IsActive
        );
    }
}
