using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Domain.Entities;

public class ServiceOrderStatusHistory(
    Guid id,
    Guid serviceOrderId,
    ServiceOrderStatus previousStatus,
    ServiceOrderStatus currentStatus,
    Guid changedBy,
    DateTime changedAt
)
{
    public Guid Id { get; private set; } = id;
    public Guid ServiceOrderId { get; private set; } = serviceOrderId;
    public ServiceOrderStatus PreviousStatus { get; private set; } = previousStatus;
    public ServiceOrderStatus CurrentStatus { get; private set; } = currentStatus;
    public Guid ChangedBy { get; private set; } = changedBy;
    public DateTime ChangedAt { get; private set; } = changedAt;
}
