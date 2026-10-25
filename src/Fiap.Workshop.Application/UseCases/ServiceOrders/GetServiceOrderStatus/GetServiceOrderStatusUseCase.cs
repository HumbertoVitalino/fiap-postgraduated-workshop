using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrderStatus.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrderStatus.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrderStatus;

public sealed class GetServiceOrderStatusUseCase(
    IServiceOrderRepository serviceOrderRepository,
    ILogger<GetServiceOrderStatusUseCase> logger
) : IGetServiceOrderStatusUseCase
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly ILogger<GetServiceOrderStatusUseCase> _logger = logger;

    public async Task<Output> Handle(GetServiceOrderStatusInput input, CancellationToken cancellationToken)
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

        output.AddResult(serviceOrder.MapToStatusDto());

        return output;
    }
}
