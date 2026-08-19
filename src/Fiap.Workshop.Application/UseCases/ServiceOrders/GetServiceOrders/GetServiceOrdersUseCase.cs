using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrders.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrders;

public sealed class GetServiceOrdersUseCase(
    IServiceOrderRepository serviceOrderRepository,
    ILogger<GetServiceOrdersUseCase> logger
) : IGetServiceOrdersUseCase
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly ILogger<GetServiceOrdersUseCase> _logger = logger;

    public async Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken)
    {
        Output output = new();

        var serviceOrders = await _serviceOrderRepository.GetAllAsync(cancellationToken);
        if (serviceOrders.Count == 0)
        {
            _logger.LogWarning("[{CorrelationId}] | Unable to find service orders", correlationId);

            output.AddResult(Array.Empty<ServiceOrderResponse>());
            return output;
        }

        output.AddResult(serviceOrders.MapToDto());
        return output;
    }
}
