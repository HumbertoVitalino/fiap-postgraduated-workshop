using Fiap.Workshop.Domain.Abstractions;

namespace Fiap.Workshop.Domain.Entities;

public class Service(
    Guid id,
    string code,
    string name,
    string description,
    decimal basePrice,
    short estimatedDuration,
    bool isActive,
    DateTime createdAt,
    DateTime updatedAt
) : AggregateRoot(id, createdAt, updatedAt)
{
    public string Code { get; private set; } = code;
    public string Name { get; private set; } = name;
    public string Description { get; private set; } = description;
    public decimal BasePrice { get; private set; } = basePrice;
    public short EstimatedDuration { get; private set; } = estimatedDuration;
    public bool IsActive { get; private set; } = isActive;
}
