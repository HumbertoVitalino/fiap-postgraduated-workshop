using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.RejectServiceOrder.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class RejectServiceOrderMapper
{
    public static RejectServiceOrderInput MapToInput(this RejectServiceOrderRequest request, Guid serviceOrderId, Guid changedBy)
    {
        return new(
            request.CorrelationId,
            serviceOrderId,
            changedBy
        );
    }
}
