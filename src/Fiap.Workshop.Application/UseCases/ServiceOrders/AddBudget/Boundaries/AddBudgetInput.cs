namespace Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Boundaries;

public sealed record AddBudgetServiceItem(Guid ServiceId, int Quantity);

public sealed record AddBudgetPartItem(Guid InventoryItemId, int Quantity);

public sealed record AddBudgetInput(
    Guid CorrelationId,
    Guid ServiceOrderId,
    Guid ChangedBy,
    IReadOnlyCollection<AddBudgetServiceItem> Services,
    IReadOnlyCollection<AddBudgetPartItem> Parts
);
