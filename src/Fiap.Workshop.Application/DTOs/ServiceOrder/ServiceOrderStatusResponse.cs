namespace Fiap.Workshop.Application.DTOs.ServiceOrder;

public sealed record ServiceOrderStatusResponse(
    Guid Id,
    string Status,
    DateTime StatusChangedAt
);
