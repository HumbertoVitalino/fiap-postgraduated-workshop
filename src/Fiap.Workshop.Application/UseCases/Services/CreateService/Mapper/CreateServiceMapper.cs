using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.Services.CreateService.Mapper;

public static class CreateServiceMapper
{
    public static Service MapToDomain(this CreateServiceInput input)
    {
        return new(
            Guid.NewGuid(),
            input.Code,
            input.Name,
            input.Description,
            input.BasePrice,
            input.EstimatedDuration,
            true,
            DateTime.Now,
            DateTime.Now
        );
    }

    public static ServiceResponse MapToDto(this Service service)
    {
        return new(
            service.Id,
            service.Code,
            service.Name,
            service.Description,
            service.BasePrice,
            service.EstimatedDuration,
            service.IsActive
        );
    }
}
