using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.UseCases.CreateVehicle.Boundaries;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.CreateVehicle.Mapper;

public static class CreateVehicleMapper
{
    public static Vehicle MapToDomain(this CreateVehicleInput input)
    {
        return new(
            Guid.NewGuid(),
            input.CustomerId,
            input.LicensePlate,
            input.Brand,
            input.Model,
            input.ManufactureYear,
            input.ModelYear,
            input.Color,
            DateTime.Now,
            DateTime.Now
        );
    }

    public static VehicleResponse MapToDto(this Vehicle vehicle)
    {
        return new(
            vehicle.Id,
            vehicle.CustomerId,
            vehicle.LicensePlate,
            vehicle.Brand,
            vehicle.Model,
            vehicle.ManufactureYear,
            vehicle.ModelYear,
            vehicle.Color
        );
    }
}
