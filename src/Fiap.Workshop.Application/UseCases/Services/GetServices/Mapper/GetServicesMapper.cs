using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Mapper;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.Services.GetServices.Mapper;

public static class GetServicesMapper
{
    public static IEnumerable<ServiceResponse> MapToDto(this IEnumerable<Service> services) => services.Select(x => x.MapToDto());
}
