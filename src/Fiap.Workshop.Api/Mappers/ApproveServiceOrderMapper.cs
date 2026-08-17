using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ApproveServiceOrder.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class ApproveServiceOrderMapper
{
    public static ApproveServiceOrderInput MapToInput(this ApproveServiceOrderRequest request, Guid serviceOrderId, Guid changedBy)
    {
        return new(
            request.CorrelationId,
            serviceOrderId,
            changedBy
        );
    }
}
