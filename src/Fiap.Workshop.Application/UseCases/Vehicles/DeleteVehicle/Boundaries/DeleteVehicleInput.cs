namespace Fiap.Workshop.Application.UseCases.Vehicles.DeleteVehicle.Boundaries;

public readonly struct DeleteVehicleInput(
    Guid correlationId,
    Guid vehicleId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid VehicleId { get; init; } = vehicleId;
}
