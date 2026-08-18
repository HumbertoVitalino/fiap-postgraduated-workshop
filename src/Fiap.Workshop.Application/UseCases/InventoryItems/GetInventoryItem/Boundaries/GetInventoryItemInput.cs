namespace Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItem.Boundaries;

public readonly struct GetInventoryItemInput(
    Guid correlationId,
    Guid inventoryItemId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid InventoryItemId { get; init; } = inventoryItemId;
}
