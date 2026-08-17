namespace Fiap.Workshop.Application.UseCases.InventoryItems.DeleteInventoryItem.Boundaries;

public readonly struct DeleteInventoryItemInput(
    Guid correlationId,
    Guid inventoryItemId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid InventoryItemId { get; init; } = inventoryItemId;
}
