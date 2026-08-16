using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class AddBudgetMapper
{
    public static AddBudgetInput MapToInput(this AddBudgetRequest request, Guid serviceOrderId, Guid changedBy)
    {
        return new(
            request.CorrelationId,
            serviceOrderId,
            changedBy,
            request.Services.Select(service => new AddBudgetServiceItem(service.ServiceId, service.Quantity)).ToList(),
            request.Parts.Select(part => new AddBudgetPartItem(part.InventoryItemId, part.Quantity)).ToList()
        );
    }
}
