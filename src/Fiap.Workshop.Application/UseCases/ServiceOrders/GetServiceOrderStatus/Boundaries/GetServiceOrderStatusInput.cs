namespace Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrderStatus.Boundaries;

public readonly struct GetServiceOrderStatusInput(
    Guid correlationId,
    Guid serviceOrderId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid ServiceOrderId { get; init; } = serviceOrderId;
}
