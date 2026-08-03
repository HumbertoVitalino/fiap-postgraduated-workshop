using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Domain.Errors;

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
    string? diagnoseDescription = null,
    decimal discount = 0m,
    DateTime? closedAt = null,
    IEnumerable<ServiceOrderPart>? parts = null,
    IEnumerable<ServiceOrderService>? services = null,
    IEnumerable<ServiceOrderStatusHistory>? statusHistory = null
) : AggregateRoot(id, createdAt, updatedAt)
{
    private static readonly Dictionary<ServiceOrderStatus, ServiceOrderStatus[]> AllowedTransitions = new()
    {
        [ServiceOrderStatus.Received] = [ServiceOrderStatus.Diagnosing, ServiceOrderStatus.Cancelled],
        [ServiceOrderStatus.Diagnosing] = [ServiceOrderStatus.AwaitingApproval, ServiceOrderStatus.Cancelled],
        [ServiceOrderStatus.AwaitingApproval] = [ServiceOrderStatus.InProgress, ServiceOrderStatus.Cancelled],
        [ServiceOrderStatus.InProgress] = [ServiceOrderStatus.Completed, ServiceOrderStatus.Cancelled],
        [ServiceOrderStatus.Completed] = [ServiceOrderStatus.Delivered],
        [ServiceOrderStatus.Delivered] = [],
        [ServiceOrderStatus.Cancelled] = [],
    };

    private readonly List<ServiceOrderPart> _parts = parts?.ToList() ?? [];
    private readonly List<ServiceOrderService> _services = services?.ToList() ?? [];
    private readonly List<ServiceOrderStatusHistory> _statusHistory = statusHistory?.ToList() ?? [];

    public int Number { get; private set; } = number;
    public Guid CustomerId { get; private set; } = customerId;
    public Guid VehicleId { get; private set; } = vehicleId;
    public Guid CreatedBy { get; private set; } = createdBy;
    public ServiceOrderStatus Status { get; private set; } = status;
    public string ProblemDescription { get; private set; } = problemDescription;
    public string? DiagnoseDescription { get; private set; } = diagnoseDescription;
    public int OdometerReading { get; private set; } = odometerReading;
    public decimal Discount { get; private set; } = discount;
    public DateTime OpenedAt { get; private set; } = openedAt;
    public DateTime? ClosedAt { get; private set; } = closedAt;

    public IReadOnlyList<ServiceOrderPart> Parts => _parts.AsReadOnly();
    public IReadOnlyList<ServiceOrderService> Services => _services.AsReadOnly();
    public IReadOnlyList<ServiceOrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public decimal Subtotal => _parts.Sum(p => p.UnitPrice * p.Quantity) + _services.Sum(s => s.UnitPrice * s.Quantity);
    public decimal Total => Subtotal - Discount;

    public void AddDiagnose(string diagnoseDescription)
    {
        EnsureIsOpen();
        DiagnoseDescription = diagnoseDescription;
        SetUpdatedAt();
    }

    public void ApplyDiscount(decimal discount)
    {
        EnsureCanModifyComposition();

        if (discount < 0 || discount > Subtotal)
            throw new DomainException(ServiceOrderErrors.InvalidDiscount);

        Discount = discount;
        SetUpdatedAt();
    }

    public void AddPart(Guid inventoryItemId, string name, string description, decimal unitPrice, int quantity)
    {
        EnsureCanModifyComposition();

        var now = DateTime.UtcNow;
        _parts.Add(new ServiceOrderPart(Guid.NewGuid(), Id, inventoryItemId, name, description, unitPrice, quantity, now, now));
        SetUpdatedAt();
    }

    public void RemovePart(Guid partId)
    {
        EnsureCanModifyComposition();

        var part = _parts.FirstOrDefault(p => p.Id == partId)
            ?? throw new DomainException(ServiceOrderErrors.PartNotFound);

        _parts.Remove(part);
        SetUpdatedAt();
    }

    public void AddService(Guid serviceId, string name, string description, decimal unitPrice, short estimatedDuration, int quantity)
    {
        EnsureCanModifyComposition();

        var now = DateTime.UtcNow;
        _services.Add(new ServiceOrderService(Guid.NewGuid(), Id, serviceId, name, description, unitPrice, estimatedDuration, quantity, now, now));
        SetUpdatedAt();
    }

    public void RemoveService(Guid serviceOrderServiceId)
    {
        EnsureCanModifyComposition();

        var service = _services.FirstOrDefault(s => s.Id == serviceOrderServiceId)
            ?? throw new DomainException(ServiceOrderErrors.ServiceNotFound);

        _services.Remove(service);
        SetUpdatedAt();
    }

    public void ChangeStatus(ServiceOrderStatus newStatus, Guid changedBy)
    {
        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new DomainException(string.Format(ServiceOrderErrors.InvalidStatusTransition, Status, newStatus));

        var previousStatus = Status;
        Status = newStatus;
        _statusHistory.Add(new ServiceOrderStatusHistory(Guid.NewGuid(), Id, previousStatus, newStatus, changedBy, DateTime.UtcNow));

        if (newStatus is ServiceOrderStatus.Delivered or ServiceOrderStatus.Cancelled)
            ClosedAt = DateTime.UtcNow;

        SetUpdatedAt();
    }

    public void Cancel(Guid changedBy) => ChangeStatus(ServiceOrderStatus.Cancelled, changedBy);

    private void EnsureCanModifyComposition()
    {
        if (Status is ServiceOrderStatus.Completed or ServiceOrderStatus.Delivered or ServiceOrderStatus.Cancelled)
            throw new DomainException(ServiceOrderErrors.OrderClosed);
    }

    private void EnsureIsOpen()
    {
        if (Status is ServiceOrderStatus.Delivered or ServiceOrderStatus.Cancelled)
            throw new DomainException(ServiceOrderErrors.OrderClosed);
    }
}
