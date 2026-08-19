using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.DeliverServiceOrder.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class DeliverServiceOrderMapper
{
    public static DeliverServiceOrderInput MapToInput(this DeliverServiceOrderRequest request, Guid serviceOrderId, Guid changedBy)
    {
        return new(
            request.CorrelationId,
            serviceOrderId,
            changedBy
        );
    }
}
