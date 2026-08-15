namespace Fiap.Workshop.Application.UseCases.Vehicles.GetVehicle.Boundaries;

public readonly struct GetVehicleInput(
    Guid correlationId,
    Guid vehicleId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid VehicleId { get; init; } = vehicleId;
}
