using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Mapper;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrder.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrder;

public sealed class GetServiceOrderUseCase(
    IServiceOrderRepository serviceOrderRepository,
    ILogger<GetServiceOrderUseCase> logger
) : IGetServiceOrderUseCase
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly ILogger<GetServiceOrderUseCase> _logger = logger;

    public async Task<Output> Handle(GetServiceOrderInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var serviceOrder = await _serviceOrderRepository.GetByIdAsync(input.ServiceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Unable to find service order by [{ServiceOrderId}].",
                input.CorrelationId,
                input.ServiceOrderId
            );

            output.AddErrorMessage("Unable to find service order");
            return output;
        }

        output.AddResult(serviceOrder.MapToDto());

        return output;
    }
}
