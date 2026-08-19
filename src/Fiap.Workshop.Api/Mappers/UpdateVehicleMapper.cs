using Fiap.Workshop.Api.Requests.Vehicles;
using Fiap.Workshop.Application.UseCases.Vehicles.UpdateVehicle.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class UpdateVehicleMapper
{
    public static UpdateVehicleInput MapToInput(this UpdateVehicleRequest request, Guid vehicleId)
    {
        return new(
            request.CorrelationId,
            vehicleId,
            request.Brand,
            request.Model,
            request.Color,
            request.ModelYear
        );
    }
}
