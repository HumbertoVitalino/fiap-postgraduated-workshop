namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class VehicleModel : Model
{
    public Guid CustomerId { get; private set; }
    public string LicensePlate { get; private set; } = default!;
    public string Brand { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public int ManufactureYear { get; private set; }
    public int ModelYear { get; private set; }
    public string Color { get; private set; } = default!;

    private VehicleModel() { }

    public VehicleModel(
        Guid id,
        Guid customerId,
        string licensePlate,
        string brand,
        string model,
        int manufactureYear,
        int modelYear,
        string color,
        DateTime createdAt,
        DateTime updatedAt
    ) : base(id, createdAt, updatedAt)
    {
        CustomerId = customerId;
        LicensePlate = licensePlate;
        Brand = brand;
        Model = model;
        ManufactureYear = manufactureYear;
        ModelYear = modelYear;
        Color = color;
    }
}
