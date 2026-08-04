using Fiap.Workshop.Domain.Abstractions;

namespace Fiap.Workshop.Domain.Entities;

public class Vehicle(
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
) : AggregateRoot(id, createdAt, updatedAt)
{
    public Guid CustomerId { get; private set; } = customerId;
    public string LicensePlate { get; private set; } = licensePlate;
    public string Brand { get; private set; } = brand;
    public string Model { get; private set; } = model;
    public int ManufactureYear { get; private set; } = manufactureYear;
    public int ModelYear { get; private set; } = modelYear;
    public string Color { get; private set; } = color;
}
