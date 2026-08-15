using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Application.DTOs.InventoryItem;

public sealed record InventoryItemResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int QuantityOnHand,
    int ReservedQuantity,
    int MinimumStock,
    decimal UnitPrice,
    UnitOfMeasure UnitOfMeasure,
    bool IsActive
);
