namespace Fiap.Workshop.Application.DTOs.ServiceOrder;

public sealed record ServiceOrderStatusHistoryResponse(
    Guid Id,
    string PreviousStatus,
    string CurrentStatus,
    Guid ChangedBy,
    DateTime ChangedAt
);
