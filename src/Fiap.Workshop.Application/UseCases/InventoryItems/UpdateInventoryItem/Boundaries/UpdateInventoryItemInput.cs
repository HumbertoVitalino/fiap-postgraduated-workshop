using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Application.UseCases.InventoryItems.UpdateInventoryItem.Boundaries;

public sealed class UpdateInventoryItemInput(
    Guid correlationId,
    Guid inventoryItemId,
    string name,
    string description,
    decimal unitPrice,
    int minimumStock,
    UnitOfMeasure unitOfMeasure,
    bool isActive
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid InventoryItemId { get; init; } = inventoryItemId;
    public string Name { get; init; } = name;
    public string Description { get; init; } = description;
    public decimal UnitPrice { get; init; } = unitPrice;
    public int MinimumStock { get; init; } = minimumStock;
    public UnitOfMeasure UnitOfMeasure { get; init; } = unitOfMeasure;
    public bool IsActive { get; init; } = isActive;
}
