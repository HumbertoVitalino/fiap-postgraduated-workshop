using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class ServiceOrderModel : Model
{
    public int Number { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public ServiceOrderStatus Status { get; private set; }
    public string ProblemDescription { get; private set; } = default!;
    public string? DiagnoseDescription { get; private set; }
    public int OdometerReading { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Total { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public List<ServiceOrderPartModel> Parts { get; private set; } = [];
    public List<ServiceOrderServiceModel> Services { get; private set; } = [];
    public List<ServiceOrderStatusHistoryModel> StatusHistory { get; private set; } = [];

    private ServiceOrderModel() { }

    public ServiceOrderModel(
        Guid id,
        int number,
        Guid customerId,
        Guid vehicleId,
        Guid createdBy,
        ServiceOrderStatus status,
        string problemDescription,
        string? diagnoseDescription,
        int odometerReading,
        decimal discount,
        decimal subtotal,
        decimal total,
        DateTime openedAt,
        DateTime? closedAt,
        DateTime createdAt,
        DateTime updatedAt
    ) : base(id, createdAt, updatedAt)
    {
        Number = number;
        CustomerId = customerId;
        VehicleId = vehicleId;
        CreatedBy = createdBy;
        Status = status;
        ProblemDescription = problemDescription;
        DiagnoseDescription = diagnoseDescription;
        OdometerReading = odometerReading;
        Discount = discount;
        Subtotal = subtotal;
        Total = total;
        OpenedAt = openedAt;
        ClosedAt = closedAt;
    }
}
