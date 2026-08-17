namespace Fiap.Workshop.Application.DTOs.ServiceOrder;

public sealed record ServiceOrderServiceResponse(
    Guid Id,
    Guid ServiceId,
    string Name,
    string Description,
    decimal UnitPrice,
    int Quantity,
    short EstimatedDuration,
    short? ActualDuration
);
