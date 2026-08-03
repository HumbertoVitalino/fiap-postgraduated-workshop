using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Errors;

namespace Fiap.Workshop.Domain.Entities;

public class ServiceOrderService
{
    public Guid Id { get; private set; }
    public Guid ServiceOrderId { get; private set; }
    public Guid ServiceId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public short EstimatedDuration { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ServiceOrderService(
        Guid id,
        Guid serviceOrderId,
        Guid serviceId,
        string name,
        string description,
        decimal unitPrice,
        short estimatedDuration,
        int quantity,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (quantity <= 0)
            throw new DomainException(ServiceOrderErrors.InvalidQuantity);

        if (unitPrice < 0)
            throw new DomainException(ServiceOrderErrors.InvalidUnitPrice);

        if (estimatedDuration < 0)
            throw new DomainException(ServiceOrderErrors.InvalidEstimatedDuration);

        Id = id;
        ServiceOrderId = serviceOrderId;
        ServiceId = serviceId;
        Name = name;
        Description = description;
        UnitPrice = unitPrice;
        EstimatedDuration = estimatedDuration;
        Quantity = quantity;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
