using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Domain.Entities;

public class ServiceOrder(
    Guid id,
    int number,
    Guid customerId,
    Guid vehicleId,
    Guid createdBy,
    string problemDescription,
    int odometerReading,
    DateTime createdAt,
    DateTime updatedAt,
    DateTime openedAt,
    ServiceOrderStatus status = ServiceOrderStatus.Received,
    string? diagnoseDescription = default,
    decimal discount = default,
    decimal subtotal = default,
    decimal total = default,
    DateTime? closedAt = null
) : AggregateRoot(id, createdAt, updatedAt)
{

    public int Number { get; private set; } = number;
    public Guid CustomerId { get; private set; } = customerId;
    public Guid VehicleId { get; private set; } = vehicleId;
    public Guid CreatedBy { get; private set; } = createdBy;
    public ServiceOrderStatus Status { get; private set; } = status;
    public string ProblemDescription { get; private set; } = problemDescription;
    public string? DiagnoseDescription { get; private set; } = diagnoseDescription;
    public int OdometerReading { get; private set; } = odometerReading;
    public decimal Discount { get; private set; } = discount;
    public decimal Subtotal { get; private set; } = subtotal;
    public decimal Total { get; private set; } = total;
    public DateTime OpenedAt { get; private set; } = openedAt;
    public DateTime? ClosedAt { get; private set; } = closedAt;

    private readonly List<ServiceOrderPart> _parts = [];
    public IReadOnlyCollection<ServiceOrderPart> Parts => _parts.AsReadOnly();

    private readonly List<ServiceOrderService> _services = [];
    public IReadOnlyCollection<ServiceOrderService> Services => _services.AsReadOnly();

    private readonly List<ServiceOrderStatusHistory> _statusHistory = [];
    public IReadOnlyCollection<ServiceOrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public void AddParts(IEnumerable<ServiceOrderPart> parts) => _parts.AddRange(parts);

    public void AddServices(IEnumerable<ServiceOrderService> services) => _services.AddRange(services);

    public void AddStatusHistory(IEnumerable<ServiceOrderStatusHistory> statusHistory) => _statusHistory.AddRange(statusHistory);
}
