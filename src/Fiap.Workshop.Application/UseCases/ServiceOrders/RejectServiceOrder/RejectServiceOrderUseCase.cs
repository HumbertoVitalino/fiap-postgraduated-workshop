using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Mapper;
using Fiap.Workshop.Application.UseCases.ServiceOrders.RejectServiceOrder.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.RejectServiceOrder;

public sealed class RejectServiceOrderUseCase(
    IServiceOrderRepository serviceOrderRepository,
    IInventoryItemRepository inventoryItemRepository,
    ILogger<RejectServiceOrderUseCase> logger
) : IRejectServiceOrderUseCase
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly IInventoryItemRepository _inventoryItemRepository = inventoryItemRepository;
    private readonly ILogger<RejectServiceOrderUseCase> _logger = logger;

    public async Task<Output> Handle(RejectServiceOrderInput input, CancellationToken cancellationToken)
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
        foreach (var part in serviceOrder.Parts)
        {
            var inventoryItem = await _inventoryItemRepository.GetByIdAsync(part.InventoryItemId, cancellationToken);
            if (inventoryItem is null)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] | Unable to find inventory item by [{InventoryItemId}].",
                    input.CorrelationId,
                    part.InventoryItemId
                );

                output.AddErrorMessage("Unable to find inventory item");
                return output;
            }

            inventoryItem.ReleaseReservation(part.Quantity);
            inventoryItems.Add(inventoryItem);
        }

        serviceOrder.Reject(input.ChangedBy);

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
