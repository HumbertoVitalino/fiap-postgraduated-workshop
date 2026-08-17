using Fiap.Workshop.Api.Requests.InventoryItems;
using Fiap.Workshop.Application.UseCases.InventoryItems.UpdateInventoryItem.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class UpdateInventoryItemMapper
{
    public static UpdateInventoryItemInput MapToInput(this UpdateInventoryItemRequest request, Guid inventoryItemId)
    {
        return new(
            request.CorrelationId,
            inventoryItemId,
            request.Name,
            request.Description,
            request.UnitPrice,
            request.MinimumStock,
            request.UnitOfMeasure,
            request.IsActive
        );
    }
}
