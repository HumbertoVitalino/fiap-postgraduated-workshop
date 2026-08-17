using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Mapper;

public static class CreateServiceOrderMapper
{
    public static ServiceOrder MapToDomain(this CreateServiceOrderInput input)
    {
        return new(
            Guid.NewGuid(),
            input.CustomerId,
            input.VehicleId,
            input.CreatedBy,
            input.ProblemDescription,
            input.OdometerReading,
            DateTime.Now,
            DateTime.Now,
            DateTime.Now
        );
    }

    public static ServiceOrderResponse MapToDto(this ServiceOrder serviceOrder)
    {
        return new(
            serviceOrder.Id,
            serviceOrder.CustomerId,
            serviceOrder.VehicleId,
            serviceOrder.CreatedBy,
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
            serviceOrder.StatusHistory.Select(history => history.MapToDto()).ToList()
        );
    }

    public static ServiceOrderPartResponse MapToDto(this ServiceOrderPart part)
    {
        return new(
            part.Id,
            part.InventoryItemId,
            part.Name,
            part.Description,
            part.UnitPrice,
            part.Quantity
        );
    }

    public static ServiceOrderServiceResponse MapToDto(this ServiceOrderService service)
    {
        return new(
            service.Id,
            service.ServiceId,
            service.Name,
            service.Description,
            service.UnitPrice,
            service.Quantity,
            service.EstimatedDuration,
            service.ActualDuration
        );
    }

    public static ServiceOrderStatusHistoryResponse MapToDto(this ServiceOrderStatusHistory statusHistory)
    {
        return new(
            statusHistory.Id,
            statusHistory.PreviousStatus.ToString(),
            statusHistory.CurrentStatus.ToString(),
            statusHistory.ChangedBy,
            statusHistory.ChangedAt
        );
    }
}
