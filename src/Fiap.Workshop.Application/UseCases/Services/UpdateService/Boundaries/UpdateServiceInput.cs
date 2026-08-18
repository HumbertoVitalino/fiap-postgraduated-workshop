namespace Fiap.Workshop.Application.UseCases.Services.UpdateService.Boundaries;

public sealed class UpdateServiceInput(
    Guid correlationId,
    Guid serviceId,
    string name,
    string description,
    decimal basePrice,
    short estimatedDuration,
    bool isActive
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid ServiceId { get; init; } = serviceId;
    public string Name { get; init; } = name;
    public string Description { get; init; } = description;
    public decimal BasePrice { get; init; } = basePrice;
    public short EstimatedDuration { get; init; } = estimatedDuration;
    public bool IsActive { get; init; } = isActive;
}
