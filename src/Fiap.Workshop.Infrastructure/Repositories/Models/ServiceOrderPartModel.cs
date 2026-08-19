namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class ServiceOrderPartModel : Model
{
    public Guid ServiceOrderId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    private ServiceOrderPartModel() { }

    public ServiceOrderPartModel(
        Guid id,
        Guid serviceOrderId,
        Guid inventoryItemId,
        string name,
        string description,
        decimal unitPrice,
        int quantity,
        DateTime createdAt,
        DateTime updatedAt
    ) : base(id, createdAt, updatedAt)
    {
        ServiceOrderId = serviceOrderId;
        InventoryItemId = inventoryItemId;
        Name = name;
        Description = description;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
