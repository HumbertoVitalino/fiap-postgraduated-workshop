namespace Fiap.Workshop.Api.Requests.ServiceOrders;

public sealed record AddBudgetServiceItemRequest(Guid ServiceId, int Quantity);

public sealed record AddBudgetPartItemRequest(Guid InventoryItemId, int Quantity);

public sealed record AddBudgetRequest(
    Guid CorrelationId,
    IReadOnlyCollection<AddBudgetServiceItemRequest> Services,
    IReadOnlyCollection<AddBudgetPartItemRequest> Parts
);
