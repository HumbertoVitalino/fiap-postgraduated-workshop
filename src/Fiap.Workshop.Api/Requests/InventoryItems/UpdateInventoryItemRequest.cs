using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Api.Requests.InventoryItems;

public sealed record UpdateInventoryItemRequest(
    Guid CorrelationId,
    string Name,
    string Description,
    decimal UnitPrice,
    int MinimumStock,
    UnitOfMeasure UnitOfMeasure,
    bool IsActive
);
