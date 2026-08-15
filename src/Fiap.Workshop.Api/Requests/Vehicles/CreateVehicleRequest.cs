namespace Fiap.Workshop.Api.Requests.Vehicles;

public sealed record CreateVehicleRequest(
    Guid CorrelationId,
    Guid CustomerId,
    string LicensePlate,
    string Brand,
    string Model,
    int ManufactureYear,
    int ModelYear,
    string Color
);
