using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class CreateServiceOrderMapper
{
    public static CreateServiceOrderInput MapToInput(this CreateServiceOrderRequest request, Guid createdBy)
    {
        return new(
            request.CorrelationId,
            request.CustomerId,
            request.VehicleId,
            createdBy,
            request.ProblemDescription,
            request.OdometerReading
        );
    }
}
