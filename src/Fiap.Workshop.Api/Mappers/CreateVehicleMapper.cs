using Fiap.Workshop.Api.Requests.Vehicles;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class CreateVehicleMapper
{
    public static CreateVehicleInput MapToInput(this CreateVehicleRequest request)
    {
        return new(
            request.CorrelationId,
            request.CustomerId,
            request.LicensePlate,
            request.Brand,
            request.Model,
            request.ManufactureYear,
            request.ModelYear,
            request.Color
        );
    }
}
