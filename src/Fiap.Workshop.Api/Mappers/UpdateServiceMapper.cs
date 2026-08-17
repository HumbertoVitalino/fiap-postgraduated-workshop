using Fiap.Workshop.Api.Requests.Services;
using Fiap.Workshop.Application.UseCases.Services.UpdateService.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class UpdateServiceMapper
{
    public static UpdateServiceInput MapToInput(this UpdateServiceRequest request, Guid serviceId)
    {
        return new(
            request.CorrelationId,
            serviceId,
            request.Name,
            request.Description,
            request.BasePrice,
            request.EstimatedDuration,
            request.IsActive
        );
    }
}
