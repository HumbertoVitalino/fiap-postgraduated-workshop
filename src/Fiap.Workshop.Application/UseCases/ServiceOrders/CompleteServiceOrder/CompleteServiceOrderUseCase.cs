using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CompleteServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Mapper;
using Fiap.Workshop.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.CompleteServiceOrder;

public sealed class CompleteServiceOrderUseCase(
    IServiceOrderRepository serviceOrderRepository,
    IServiceRepository serviceRepository,
    ILogger<CompleteServiceOrderUseCase> logger
) : ICompleteServiceOrderUseCase
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly IServiceRepository _serviceRepository = serviceRepository;
    private readonly ILogger<CompleteServiceOrderUseCase> _logger = logger;

    public async Task<Output> Handle(CompleteServiceOrderInput input, CancellationToken cancellationToken)
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

        serviceOrder.Complete(
            input.ServiceDurations.Select(duration => (duration.ServiceOrderServiceId, duration.ActualDuration)).ToList(),
            input.ChangedBy
        );

        var services = new List<Service>();
        foreach (var orderService in serviceOrder.Services)
        {
            var service = await _serviceRepository.GetByIdAsync(orderService.ServiceId, cancellationToken);
            if (service is null)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] | Unable to find service by [{ServiceId}].",
                    input.CorrelationId,
                    orderService.ServiceId
                );

                output.AddErrorMessage("Unable to find service");
                return output;
            }

            service.RecordExecution(orderService.ActualDuration!.Value);
            services.Add(service);
        }

        await _serviceOrderRepository.UpdateAsync(serviceOrder, cancellationToken);

        foreach (var service in services)
            _serviceRepository.Update(service);

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
