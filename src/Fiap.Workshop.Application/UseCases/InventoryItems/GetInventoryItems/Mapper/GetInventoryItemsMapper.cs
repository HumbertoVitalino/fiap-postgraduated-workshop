using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Mapper;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItems.Mapper;

public static class GetInventoryItemsMapper
{
    public static IEnumerable<InventoryItemResponse> MapToDto(this IEnumerable<InventoryItem> inventoryItems) => inventoryItems.Select(x => x.MapToDto());
}
