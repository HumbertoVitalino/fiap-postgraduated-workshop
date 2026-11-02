using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Domain.Errors;
using Fiap.Workshop.Domain.Events;

namespace Fiap.Workshop.Domain.Entities;

public class ServiceOrder(
    Guid id,
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

    public void StartDiagnosis(string diagnoseDescription, Guid changedBy)
    {
        EnsureCanTransitionTo(ServiceOrderStatus.Diagnosing, ServiceOrderStatus.Received);

        DiagnoseDescription = diagnoseDescription;

        ChangeStatus(ServiceOrderStatus.Diagnosing, changedBy);
    }

    public void AddBudget(IEnumerable<ServiceOrderService> services, IEnumerable<ServiceOrderPart> parts, Guid changedBy)
    {
        EnsureCanTransitionTo(ServiceOrderStatus.AwaitingApproval, ServiceOrderStatus.Diagnosing);

        var servicesList = services.ToList();
        var partsList = parts.ToList();

        AddServices(servicesList);
        AddParts(partsList);

        Subtotal = servicesList.Sum(service => service.UnitPrice * service.Quantity)
            + partsList.Sum(part => part.UnitPrice * part.Quantity);
        Total = Subtotal;

        ChangeStatus(ServiceOrderStatus.AwaitingApproval, changedBy);
    }

    public void Approve(Guid changedBy)
    {
        EnsureCanTransitionTo(ServiceOrderStatus.InProgress, ServiceOrderStatus.AwaitingApproval);

        ChangeStatus(ServiceOrderStatus.InProgress, changedBy);
    }

    public void Reject(Guid changedBy)
    {
        EnsureCanTransitionTo(ServiceOrderStatus.Cancelled, ServiceOrderStatus.AwaitingApproval);

        ChangeStatus(ServiceOrderStatus.Cancelled, changedBy);
    }

    public void Complete(IReadOnlyCollection<(Guid ServiceOrderServiceId, short ActualDuration)> durations, Guid changedBy)
    {
        EnsureCanTransitionTo(ServiceOrderStatus.Completed, ServiceOrderStatus.InProgress);

        var serviceIds = _services.Select(service => service.Id).ToHashSet();
        var durationIds = durations.Select(duration => duration.ServiceOrderServiceId).ToHashSet();

        if (!serviceIds.SetEquals(durationIds))
            throw new DomainException(ServiceOrderErrors.MissingServiceDuration);

        var durationsById = durations.ToDictionary(duration => duration.ServiceOrderServiceId, duration => duration.ActualDuration);

        foreach (var service in _services)
            service.RecordActualDuration(durationsById[service.Id]);

        ChangeStatus(ServiceOrderStatus.Completed, changedBy);
    }

    public void Deliver(Guid changedBy)
    {
        EnsureCanTransitionTo(ServiceOrderStatus.Delivered, ServiceOrderStatus.Completed, ServiceOrderStatus.Cancelled);

        ClosedAt = DateTime.Now;

        ChangeStatus(ServiceOrderStatus.Delivered, changedBy);
    }

    private void EnsureCanTransitionTo(ServiceOrderStatus newStatus, params ReadOnlySpan<ServiceOrderStatus> allowedCurrentStatuses)
    {
        if (!allowedCurrentStatuses.Contains(Status))
            throw new DomainException(string.Format(ServiceOrderErrors.InvalidStatusTransition, Status, newStatus));
    }

    private void ChangeStatus(ServiceOrderStatus newStatus, Guid changedBy)
    {
        var previousStatus = Status;
        var changedAt = DateTime.Now;

        Status = newStatus;

        _statusHistory.Add(
            new ServiceOrderStatusHistory(
                Guid.NewGuid(),
                Id,
                previousStatus,
                newStatus,
                changedBy,
                changedAt
            )
        );

        RaiseDomainEvent(
            new ServiceOrderStatusChangedEvent(
                Id,
                CustomerId,
                previousStatus,
                newStatus,
                changedAt
            )
        );

        SetUpdatedAt();
    }
}
