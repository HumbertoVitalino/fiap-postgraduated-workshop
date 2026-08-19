namespace Fiap.Workshop.Application.UseCases.ServiceOrders.DeliverServiceOrder.Boundaries;

public sealed record DeliverServiceOrderInput(
    Guid CorrelationId,
    Guid ServiceOrderId,
    Guid ChangedBy
);
