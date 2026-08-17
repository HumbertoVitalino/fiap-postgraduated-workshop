namespace Fiap.Workshop.Application.DTOs.ServiceOrder;

public sealed record ServiceOrderTrackingStatusHistoryResponse(
    Guid Id,
    string PreviousStatus,
    string CurrentStatus,
    DateTime ChangedAt
);
