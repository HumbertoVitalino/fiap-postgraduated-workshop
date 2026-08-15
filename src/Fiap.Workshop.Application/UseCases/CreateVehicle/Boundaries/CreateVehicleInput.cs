using Fiap.Workshop.Application.Commons;

namespace Fiap.Workshop.Application.UseCases.CreateVehicle.Boundaries;

public sealed class CreateVehicleInput
{
    public Guid CorrelationId { get; init; }
    public Guid CustomerId { get; init; }
    public string LicensePlate
    {
        get;
        init => field = value.NormalizeLicensePlate();
    }
    public string Brand { get; init; }
    public string Model { get; init; }
    public int ManufactureYear { get; init; }
    public int ModelYear { get; init; }
    public string Color { get; init; }

    public CreateVehicleInput(
        Guid correlationId,
        Guid customerId,
        string licensePlate,
        string brand,
        string model,
        int manufactureYear,
        int modelYear,
        string color
    )
    {
        CorrelationId = correlationId;
        CustomerId = customerId;
        Brand = brand;
        Model = model;
        ManufactureYear = manufactureYear;
        ModelYear = modelYear;
        Color = color;
        LicensePlate = licensePlate;
    }
}
