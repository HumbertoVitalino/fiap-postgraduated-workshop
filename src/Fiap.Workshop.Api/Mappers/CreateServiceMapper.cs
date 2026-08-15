using Fiap.Workshop.Api.Requests.Services;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class CreateServiceMapper
{
    public static CreateServiceInput MapToInput(this CreateServiceRequest request)
    {
        return new(
            request.CorrelationId,
            request.Code,
            request.Name,
            request.Description,
            request.BasePrice,
            request.EstimatedDuration
        );
    }
}
