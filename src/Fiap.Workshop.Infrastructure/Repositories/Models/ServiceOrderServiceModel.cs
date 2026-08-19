namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class ServiceOrderServiceModel : Model
{
    public Guid ServiceOrderId { get; private set; }
    public Guid ServiceId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public short EstimatedDuration { get; private set; }
    public short? ActualDuration { get; private set; }

    private ServiceOrderServiceModel() { }

    public ServiceOrderServiceModel(
        Guid id,
        Guid serviceOrderId,
        Guid serviceId,
        string name,
        string description,
        decimal unitPrice,
        short estimatedDuration,
        int quantity,
        DateTime createdAt,
        DateTime updatedAt,
        short? actualDuration = null
    ) : base(id, createdAt, updatedAt)
    {
        ServiceOrderId = serviceOrderId;
        ServiceId = serviceId;
        Name = name;
        Description = description;
        UnitPrice = unitPrice;
        EstimatedDuration = estimatedDuration;
        Quantity = quantity;
        ActualDuration = actualDuration;
    }
}
