using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class InventoryItemModel : Model
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public int QuantityOnHand { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int MinimumStock { get; private set; }
    public decimal UnitPrice { get; private set; }
    public UnitOfMeasure UnitOfMeasure { get; private set; }
    public bool IsActive { get; private set; }

    private InventoryItemModel() { }

    public InventoryItemModel(
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
    ) : base(id, createdAt, updatedAt)
    {
        Code = code;
        Name = name;
        Description = description;
        QuantityOnHand = quantityOnHand;
        ReservedQuantity = reservedQuantity;
        MinimumStock = minimumStock;
        UnitPrice = unitPrice;
        UnitOfMeasure = unitOfMeasure;
        IsActive = isActive;
    }
}
