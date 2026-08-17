using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Mapper;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.Vehicles.GetVehicles.Mapper;

public static class GetVehiclesMapper
{
    public static IEnumerable<VehicleResponse> MapToDto(this IEnumerable<Vehicle> vehicles) => vehicles.Select(x => x.MapToDto());
}
