namespace Fiap.Workshop.Application.UseCases.ServiceOrders.ApproveServiceOrder.Boundaries;

public sealed record ApproveServiceOrderInput(
    Guid CorrelationId,
    Guid ServiceOrderId,
    Guid ChangedBy
);
