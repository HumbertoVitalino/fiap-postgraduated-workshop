namespace Fiap.Workshop.Api.Requests.Services;

public sealed record UpdateServiceRequest(
    Guid CorrelationId,
    string Name,
    string Description,
    decimal BasePrice,
    short EstimatedDuration,
    bool IsActive
);
