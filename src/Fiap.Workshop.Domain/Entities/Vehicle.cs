using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Errors;

namespace Fiap.Workshop.Domain.Entities;

public class Vehicle : AggregateRoot
{
    public Guid CustomerId { get; private set; }
    public string LicensePlate { get; private set; }
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public int ManufactureYear { get; private set; }
    public int ModelYear { get; private set; }
    public string Color { get; private set; }

    public Vehicle(
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
        if (modelYear < manufactureYear)
            throw new DomainException(VehicleErrors.InvalidModelYear);

        CustomerId = customerId;
        LicensePlate = licensePlate;
        Brand = brand;
        Model = model;
        ManufactureYear = manufactureYear;
        ModelYear = modelYear;
        Color = color;
    }

    public void UpdateProfile(string brand, string model, string color, int modelYear)
    {
        if (modelYear < ManufactureYear)
            throw new DomainException(VehicleErrors.InvalidModelYear);

        Brand = brand;
        Model = model;
        Color = color;
        ModelYear = modelYear;
        SetUpdatedAt();
    }
}
