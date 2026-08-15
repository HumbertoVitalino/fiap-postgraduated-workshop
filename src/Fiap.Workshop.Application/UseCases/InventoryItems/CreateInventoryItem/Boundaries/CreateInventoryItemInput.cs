using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;

public sealed record CreateInventoryItemInput(
    Guid CorrelationId,
    string Code,
    string Name,
    string Description,
    int QuantityOnHand,
    int MinimumStock,
    decimal UnitPrice,
    UnitOfMeasure UnitOfMeasure
);
