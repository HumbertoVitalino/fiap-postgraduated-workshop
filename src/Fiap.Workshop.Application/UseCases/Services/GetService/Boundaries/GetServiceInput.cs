namespace Fiap.Workshop.Application.UseCases.Services.GetService.Boundaries;

public readonly struct GetServiceInput(
    Guid correlationId,
    Guid serviceId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid ServiceId { get; init; } = serviceId;
}
