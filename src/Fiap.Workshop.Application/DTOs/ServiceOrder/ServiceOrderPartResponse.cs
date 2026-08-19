namespace Fiap.Workshop.Application.DTOs.ServiceOrder;

public sealed record ServiceOrderPartResponse(
    Guid Id,
    Guid InventoryItemId,
    string Name,
    string Description,
    decimal UnitPrice,
    int Quantity
);
