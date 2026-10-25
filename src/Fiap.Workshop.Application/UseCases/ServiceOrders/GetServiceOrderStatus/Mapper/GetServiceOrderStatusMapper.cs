using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrderStatus.Mapper;

public static class GetServiceOrderStatusMapper
{
    public static ServiceOrderStatusResponse MapToStatusDto(this ServiceOrder serviceOrder)
    {
        var statusChangedAt = serviceOrder.StatusHistory.Count == 0
            ? serviceOrder.OpenedAt
            : serviceOrder.StatusHistory.Max(history => history.ChangedAt);

        return new(
            serviceOrder.Id,
            serviceOrder.Status.ToString(),
            statusChangedAt
        );
    }
}
