using Fiap.Workshop.Application.UseCases.ServiceOrders.RejectServiceOrder.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class RejectServiceOrderMapper
{
    public static RejectServiceOrderInput MapToInput(this Guid request, Guid serviceOrderId, Guid changedBy)
    {
        return new(
            request,
            serviceOrderId,
            changedBy
        );
    }
}
