using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Errors;

namespace Fiap.Workshop.Domain.Entities;

public class Service : AggregateRoot
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public short EstimatedDuration { get; private set; }
    public bool IsActive { get; private set; }

    public Service(
        Guid id,
        string code,
        string name,
        string description,
        decimal basePrice,
        short estimatedDuration,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt
    ) : base(id, createdAt, updatedAt)
    {
        if (basePrice < 0)
            throw new DomainException(ServiceErrors.InvalidBasePrice);

        if (estimatedDuration < 0)
            throw new DomainException(ServiceErrors.InvalidEstimatedDuration);

        Code = code;
        Name = name;
        Description = description;
        BasePrice = basePrice;
        EstimatedDuration = estimatedDuration;
        IsActive = isActive;
    }
}
