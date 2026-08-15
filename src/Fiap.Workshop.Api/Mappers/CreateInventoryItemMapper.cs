using Fiap.Workshop.Api.Requests.InventoryItems;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class CreateInventoryItemMapper
{
    public static CreateInventoryItemInput MapToInput(this CreateInventoryItemRequest request)
    {
        return new(
            request.CorrelationId,
            request.Code,
            request.Name,
            request.Description,
            request.QuantityOnHand,
            request.MinimumStock,
            request.UnitPrice,
            request.UnitOfMeasure
        );
    }
}
