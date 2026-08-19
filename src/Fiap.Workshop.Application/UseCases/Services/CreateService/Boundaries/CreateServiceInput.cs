namespace Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;

public sealed record CreateServiceInput(
    Guid CorrelationId,
    string Code,
    string Name,
    string Description,
    decimal BasePrice,
    short EstimatedDuration
);
