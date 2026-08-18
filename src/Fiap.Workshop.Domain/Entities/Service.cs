using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Errors;

namespace Fiap.Workshop.Domain.Entities;

public class Service : AggregateRoot
{
    private const int ONE_MORE_EXECUTION = 1;

    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public short EstimatedDuration { get; private set; }
    public int ExecutionCount { get; private set; }
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
        DateTime updatedAt,
        int executionCount = 0
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
        ExecutionCount = executionCount;
    }

    public void RecordExecution(short actualDuration)
    {
        EstimatedDuration = (short)(EstimatedDuration + (actualDuration - EstimatedDuration) / (ExecutionCount + ONE_MORE_EXECUTION));
        ExecutionCount++;

        SetUpdatedAt();
    }

    public void UpdateProfile(string name, string description, decimal basePrice, short estimatedDuration, bool isActive)
    {
        if (basePrice < 0)
            throw new DomainException(ServiceErrors.InvalidBasePrice);

        if (estimatedDuration < 0)
            throw new DomainException(ServiceErrors.InvalidEstimatedDuration);

        Name = name;
        Description = description;
        BasePrice = basePrice;
        IsActive = isActive;

        if (estimatedDuration != EstimatedDuration)
        {
            EstimatedDuration = estimatedDuration;
            ExecutionCount = 0;
        }

        SetUpdatedAt();
    }
}
