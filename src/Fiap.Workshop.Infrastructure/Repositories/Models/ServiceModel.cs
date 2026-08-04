namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class ServiceModel : Model
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal BasePrice { get; private set; }
    public short EstimatedDuration { get; private set; }
    public bool IsActive { get; private set; }

    private ServiceModel() { }

    public ServiceModel(
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
        Code = code;
        Name = name;
        Description = description;
        BasePrice = basePrice;
        EstimatedDuration = estimatedDuration;
        IsActive = isActive;
    }
}
