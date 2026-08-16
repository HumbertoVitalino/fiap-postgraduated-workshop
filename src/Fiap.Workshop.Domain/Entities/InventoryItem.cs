using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Domain.Errors;

namespace Fiap.Workshop.Domain.Entities;

public class InventoryItem : AggregateRoot
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int QuantityOnHand { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int MinimumStock { get; private set; }
    public decimal UnitPrice { get; private set; }
    public UnitOfMeasure UnitOfMeasure { get; private set; }
    public bool IsActive { get; private set; }

    public InventoryItem(
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
        if (quantityOnHand < 0)
            throw new DomainException(InventoryItemErrors.InvalidQuantityOnHand);

        if (reservedQuantity < 0)
            throw new DomainException(InventoryItemErrors.InvalidReservedQuantity);

        if (reservedQuantity > quantityOnHand)
            throw new DomainException(InventoryItemErrors.ReservedQuantityExceedsQuantityOnHand);

        if (minimumStock < 0)
            throw new DomainException(InventoryItemErrors.InvalidMinimumStock);

        if (unitPrice < 0)
            throw new DomainException(InventoryItemErrors.InvalidUnitPrice);

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

    public void Reserve(int quantity)
    {
        if (quantity > QuantityOnHand - ReservedQuantity)
            throw new DomainException(InventoryItemErrors.InsufficientStock);

        ReservedQuantity += quantity;

        SetUpdatedAt();
    }
}
