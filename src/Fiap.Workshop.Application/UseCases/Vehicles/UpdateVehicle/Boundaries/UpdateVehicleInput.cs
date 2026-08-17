namespace Fiap.Workshop.Application.UseCases.Vehicles.UpdateVehicle.Boundaries;

public sealed class UpdateVehicleInput(
    Guid correlationId,
    Guid vehicleId,
    string brand,
    string model,
    string color,
    int modelYear
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid VehicleId { get; init; } = vehicleId;
    public string Brand { get; init; } = brand;
    public string Model { get; init; } = model;
    public string Color { get; init; } = color;
    public int ModelYear { get; init; } = modelYear;
}
