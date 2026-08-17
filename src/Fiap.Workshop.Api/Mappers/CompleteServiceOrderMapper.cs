using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CompleteServiceOrder.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class CompleteServiceOrderMapper
{
    public static CompleteServiceOrderInput MapToInput(this CompleteServiceOrderRequest request, Guid serviceOrderId, Guid changedBy)
    {
        return new(
            request.CorrelationId,
            serviceOrderId,
            changedBy,
            request.ServiceDurations
                .Select(duration => new CompleteServiceOrderDuration(duration.ServiceOrderServiceId, duration.ActualDuration))
                .ToList()
        );
    }
}
