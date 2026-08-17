namespace Fiap.Workshop.Application.UseCases.ServiceOrders.RejectServiceOrder.Boundaries;

public sealed record RejectServiceOrderInput(
    Guid CorrelationId,
    Guid ServiceOrderId,
    Guid ChangedBy
);
