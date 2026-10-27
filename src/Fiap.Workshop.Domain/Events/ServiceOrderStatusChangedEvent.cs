using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Domain.Events;

public sealed record ServiceOrderStatusChangedEvent(
    Guid ServiceOrderId,
    Guid CustomerId,
    ServiceOrderStatus PreviousStatus,
    ServiceOrderStatus CurrentStatus,
    DateTime ChangedAt
) : IDomainEvent;
