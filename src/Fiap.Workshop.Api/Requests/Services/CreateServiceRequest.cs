namespace Fiap.Workshop.Api.Requests.Services;

public sealed record CreateServiceRequest(
    Guid CorrelationId,
    string Code,
    string Name,
    string Description,
    decimal BasePrice,
    short EstimatedDuration
);
