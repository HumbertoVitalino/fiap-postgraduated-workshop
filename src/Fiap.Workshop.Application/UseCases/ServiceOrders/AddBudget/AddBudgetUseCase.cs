using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Mapper;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Mapper;
using Fiap.Workshop.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget;

public sealed class AddBudgetUseCase(
    IServiceOrderRepository serviceOrderRepository,
    IServiceRepository serviceRepository,
    IInventoryItemRepository inventoryItemRepository,
    ILogger<AddBudgetUseCase> logger
) : IAddBudgetUseCase
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly IServiceRepository _serviceRepository = serviceRepository;
    private readonly IInventoryItemRepository _inventoryItemRepository = inventoryItemRepository;
    private readonly ILogger<AddBudgetUseCase> _logger = logger;

    public async Task<Output> Handle(AddBudgetInput input, CancellationToken cancellationToken)
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

        var inventoryItems = new List<InventoryItem>();
        var parts = new List<ServiceOrderPart>();
        foreach (var partItem in input.Parts)
        {
            var inventoryItem = await _inventoryItemRepository.GetByIdAsync(partItem.InventoryItemId, cancellationToken);
            if (inventoryItem is null)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] | Unable to find inventory item by [{InventoryItemId}].",
                    input.CorrelationId,
                    partItem.InventoryItemId
                );

                output.AddErrorMessage("Unable to find inventory item");
                return output;
            }

            inventoryItem.Reserve(partItem.Quantity);

            inventoryItems.Add(inventoryItem);
            parts.Add(inventoryItem.MapToServiceOrderPart(serviceOrder.Id, partItem.Quantity));
        }

        var orderServices = new List<ServiceOrderService>();
        foreach (var serviceItem in input.Services)
        {
            var service = await _serviceRepository.GetByIdAsync(serviceItem.ServiceId, cancellationToken);
            if (service is null)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] | Unable to find service by [{ServiceId}].",
                    input.CorrelationId,
                    serviceItem.ServiceId
                );

                output.AddErrorMessage("Unable to find service");
                return output;
            }

            orderServices.Add(service.MapToServiceOrderService(serviceOrder.Id, serviceItem.Quantity));
        }

        serviceOrder.AddBudget(orderServices, parts, input.ChangedBy);

        await _serviceOrderRepository.UpdateAsync(serviceOrder, cancellationToken);

        foreach (var inventoryItem in inventoryItems)
            _inventoryItemRepository.Update(inventoryItem);

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
