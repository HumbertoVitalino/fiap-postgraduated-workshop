using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Mapper;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision;

public sealed class ProcessBudgetDecisionUseCase(
    IServiceOrderRepository serviceOrderRepository,
    IInventoryItemRepository inventoryItemRepository,
    ILogger<ProcessBudgetDecisionUseCase> logger
) : IProcessBudgetDecisionUseCase
{
    public static readonly Guid SystemUserId = Guid.Parse("2f31de13-df54-4036-a42a-59940c7796d3");
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly IInventoryItemRepository _inventoryItemRepository = inventoryItemRepository;
    private readonly ILogger<ProcessBudgetDecisionUseCase> _logger = logger;

    private sealed record DecisionRule(
        ServiceOrderStatus ResultingStatus,
        Action<ServiceOrder, Guid> ApplyToServiceOrder,
        Action<InventoryItem, int> SettleReservation
    );

    private static readonly Dictionary<BudgetDecision, DecisionRule> DecisionRules = new()
    {
        [BudgetDecision.Approved] = new(
            ServiceOrderStatus.InProgress,
            (serviceOrder, changedBy) => serviceOrder.Approve(changedBy),
            (inventoryItem, quantity) => inventoryItem.CommitReservation(quantity)
        ),
        [BudgetDecision.Rejected] = new(
            ServiceOrderStatus.Cancelled,
            (serviceOrder, changedBy) => serviceOrder.Reject(changedBy),
            (inventoryItem, quantity) => inventoryItem.ReleaseReservation(quantity)
        )
    };

    public async Task<Output> Handle(ProcessBudgetDecisionInput input, CancellationToken cancellationToken)
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

        var rule = DecisionRules[input.Decision];

        if (serviceOrder.Status == rule.ResultingStatus)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Budget decision {Decision} already applied to service order [{ServiceOrderId}].",
                input.CorrelationId,
                input.Decision,
                input.ServiceOrderId
            );

            output.AddMessage("Budget decision already applied");
            output.AddResult(serviceOrder.MapToDto());
            return output;
        }

        rule.ApplyToServiceOrder(serviceOrder, SystemUserId);

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

            rule.SettleReservation(inventoryItem, part.Quantity);
            inventoryItems.Add(inventoryItem);
        }

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
