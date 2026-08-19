using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class ServiceOrderStatusHistoryModel
{
    public Guid Id { get; private set; }
    public Guid ServiceOrderId { get; private set; }
    public ServiceOrderStatus PreviousStatus { get; private set; }
    public ServiceOrderStatus CurrentStatus { get; private set; }
    public Guid ChangedBy { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private ServiceOrderStatusHistoryModel() { }

    public ServiceOrderStatusHistoryModel(
        Guid id,
        Guid serviceOrderId,
        ServiceOrderStatus previousStatus,
        ServiceOrderStatus currentStatus,
        Guid changedBy,
        DateTime changedAt
    )
    {
        Id = id;
        ServiceOrderId = serviceOrderId;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
    }
}
