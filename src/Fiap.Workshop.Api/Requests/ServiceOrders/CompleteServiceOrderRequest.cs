namespace Fiap.Workshop.Api.Requests.ServiceOrders;

public sealed record CompleteServiceOrderDurationRequest(Guid ServiceOrderServiceId, short ActualDuration);

public sealed record CompleteServiceOrderRequest(
    Guid CorrelationId,
    IReadOnlyCollection<CompleteServiceOrderDurationRequest> ServiceDurations
);
