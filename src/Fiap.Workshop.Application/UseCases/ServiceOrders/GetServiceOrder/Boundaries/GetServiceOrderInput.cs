namespace Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrder.Boundaries;

public readonly struct GetServiceOrderInput(
    Guid correlationId,
    Guid serviceOrderId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid ServiceOrderId { get; init; } = serviceOrderId;
}
