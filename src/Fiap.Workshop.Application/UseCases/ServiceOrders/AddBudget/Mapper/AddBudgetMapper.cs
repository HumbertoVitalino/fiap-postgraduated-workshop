using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Mapper;

public static class AddBudgetMapper
{
    public static ServiceOrderPart MapToServiceOrderPart(this InventoryItem inventoryItem, Guid serviceOrderId, int quantity)
    {
        return new(
            Guid.NewGuid(),
            serviceOrderId,
            inventoryItem.Id,
            inventoryItem.Name,
            inventoryItem.Description,
            inventoryItem.UnitPrice,
            quantity,
            DateTime.Now,
            DateTime.Now
        );
    }

    public static ServiceOrderService MapToServiceOrderService(this Service service, Guid serviceOrderId, int quantity)
    {
        return new(
            Guid.NewGuid(),
            serviceOrderId,
            service.Id,
            service.Name,
            service.Description,
            service.BasePrice,
            service.EstimatedDuration,
            quantity,
            DateTime.Now,
            DateTime.Now
        );
    }
}
