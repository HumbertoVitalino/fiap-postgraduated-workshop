namespace Fiap.Workshop.Application.UseCases.ServiceOrders.CompleteServiceOrder.Boundaries;

public sealed record CompleteServiceOrderDuration(Guid ServiceOrderServiceId, short ActualDuration);

public sealed record CompleteServiceOrderInput(
    Guid CorrelationId,
    Guid ServiceOrderId,
    Guid ChangedBy,
    IReadOnlyCollection<CompleteServiceOrderDuration> ServiceDurations
);
