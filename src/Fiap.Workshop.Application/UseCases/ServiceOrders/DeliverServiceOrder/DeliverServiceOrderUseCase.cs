using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Mapper;
using Fiap.Workshop.Application.UseCases.ServiceOrders.DeliverServiceOrder.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.DeliverServiceOrder;

public sealed class DeliverServiceOrderUseCase(
    IServiceOrderRepository serviceOrderRepository,
    ILogger<DeliverServiceOrderUseCase> logger
) : IDeliverServiceOrderUseCase
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly ILogger<DeliverServiceOrderUseCase> _logger = logger;

    public async Task<Output> Handle(DeliverServiceOrderInput input, CancellationToken cancellationToken)
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

        serviceOrder.Deliver(input.ChangedBy);

        await _serviceOrderRepository.UpdateAsync(serviceOrder, cancellationToken);

        var saved = await _serviceOrderRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error updating service order with id {ServiceOrderId}.",
                input.CorrelationId,
                input.ServiceOrderId
            );

            output.AddErrorMessage($"Error updating service order with id {input.ServiceOrderId}.");
            return output;
        }

        output.AddResult(serviceOrder.MapToDto());
        return output;
    }
}
