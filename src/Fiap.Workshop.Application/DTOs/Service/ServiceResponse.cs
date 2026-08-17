namespace Fiap.Workshop.Application.DTOs.Service;

public sealed record ServiceResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    decimal BasePrice,
    short EstimatedDuration,
    int ExecutionCount,
    bool IsActive
);
