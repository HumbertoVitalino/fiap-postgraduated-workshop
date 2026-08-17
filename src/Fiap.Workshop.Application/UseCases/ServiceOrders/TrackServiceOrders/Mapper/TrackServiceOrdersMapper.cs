using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Mapper;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders.Mapper;

public static class TrackServiceOrdersMapper
{
    public static IReadOnlyCollection<ServiceOrderTrackingResponse> MapToDto(this IEnumerable<ServiceOrder> serviceOrders) =>
        serviceOrders.Select(serviceOrder => serviceOrder.MapToTrackingDto()).ToList();

    public static ServiceOrderTrackingResponse MapToTrackingDto(this ServiceOrder serviceOrder)
    {
        return new(
            serviceOrder.Id,
            serviceOrder.Status.ToString(),
            serviceOrder.ProblemDescription,
            serviceOrder.DiagnoseDescription,
            serviceOrder.OdometerReading,
            serviceOrder.Discount,
            serviceOrder.Subtotal,
            serviceOrder.Total,
            serviceOrder.OpenedAt,
            serviceOrder.ClosedAt,
            serviceOrder.Parts.Select(part => part.MapToDto()).ToList(),
            serviceOrder.Services.Select(service => service.MapToDto()).ToList(),
            serviceOrder.StatusHistory.Select(history => history.MapToTrackingDto()).ToList()
        );
    }

    public static ServiceOrderTrackingStatusHistoryResponse MapToTrackingDto(this ServiceOrderStatusHistory statusHistory)
    {
        return new(
            statusHistory.Id,
            statusHistory.PreviousStatus.ToString(),
            statusHistory.CurrentStatus.ToString(),
            statusHistory.ChangedAt
        );
    }
}
