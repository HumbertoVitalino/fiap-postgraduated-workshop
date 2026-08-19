namespace Fiap.Workshop.Api.Requests.Vehicles;

public sealed record UpdateVehicleRequest(
    Guid CorrelationId,
    string Brand,
    string Model,
    string Color,
    int ModelYear
);
