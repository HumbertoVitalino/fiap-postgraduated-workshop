using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Mapper;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrders.Mapper;

public static class GetServiceOrdersMapper
{
    public static IEnumerable<ServiceOrderResponse> MapToDto(this IEnumerable<ServiceOrder> serviceOrders) => serviceOrders.Select(x => x.MapToDto());
}
