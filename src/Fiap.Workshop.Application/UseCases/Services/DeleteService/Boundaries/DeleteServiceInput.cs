namespace Fiap.Workshop.Application.UseCases.Services.DeleteService.Boundaries;

public readonly struct DeleteServiceInput(
    Guid correlationId,
    Guid serviceId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid ServiceId { get; init; } = serviceId;
}
