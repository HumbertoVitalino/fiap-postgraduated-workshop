using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Domain.Entities;

public class InventoryItem(
    Guid id,
    string code,
    string name,
    string description,
    int quantityOnHand,
    int reservedQuantity,
    int minimumStock,
    decimal unitPrice,
    UnitOfMeasure unitOfMeasure,
    bool isActive,
    DateTime createdAt,
    DateTime updatedAt
) : AggregateRoot(id, createdAt, updatedAt)
{
    public string Code { get; private set; } = code;
    public string Name { get; private set; } = name;
    public string Description { get; private set; } = description;
    public int QuantityOnHand { get; private set; } = quantityOnHand;
    public int ReservedQuantity { get; private set; } = reservedQuantity;
    public int MinimumStock { get; private set; } = minimumStock;
    public decimal UnitPrice { get; private set; } = unitPrice;
    public UnitOfMeasure UnitOfMeasure { get; private set; } = unitOfMeasure;
    public bool IsActive { get; private set; } = isActive;
}
