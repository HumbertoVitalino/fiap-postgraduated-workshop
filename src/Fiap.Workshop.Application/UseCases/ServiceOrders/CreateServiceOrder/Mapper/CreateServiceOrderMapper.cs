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
            serviceOrder.ClosedAt
        );
    }
}
