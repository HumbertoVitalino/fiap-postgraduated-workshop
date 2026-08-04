using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Errors;

namespace Fiap.Workshop.Domain.Entities;

public class ServiceOrderPart
{
    public Guid Id { get; private set; }
    public Guid ServiceOrderId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ServiceOrderPart(
        Guid id,
        Guid serviceOrderId,
        Guid inventoryItemId,
        string name,
        string description,
        decimal unitPrice,
        int quantity,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (quantity <= 0)
            throw new DomainException(ServiceOrderErrors.InvalidQuantity);

        if (unitPrice < 0)
            throw new DomainException(ServiceOrderErrors.InvalidUnitPrice);

        Id = id;
        ServiceOrderId = serviceOrderId;
        InventoryItemId = inventoryItemId;
        Name = name;
        Description = description;
        UnitPrice = unitPrice;
        Quantity = quantity;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
