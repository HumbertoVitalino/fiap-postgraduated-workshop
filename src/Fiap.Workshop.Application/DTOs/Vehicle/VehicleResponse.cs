namespace Fiap.Workshop.Application.DTOs.Vehicle;

public sealed record VehicleResponse(
    Guid Id,
    Guid CustomerId,
    string LicensePlate,
    string Brand,
    string Model,
    int ManufactureYear,
    int ModelYear,
    string Color
);
