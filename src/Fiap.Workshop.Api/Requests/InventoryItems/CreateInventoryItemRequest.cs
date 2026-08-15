using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Api.Requests.InventoryItems;

public sealed record CreateInventoryItemRequest(
    Guid CorrelationId,
    string Code,
    string Name,
    string Description,
    int QuantityOnHand,
    int MinimumStock,
    decimal UnitPrice,
    UnitOfMeasure UnitOfMeasure
);
