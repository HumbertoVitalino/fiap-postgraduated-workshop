using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.InventoryItems;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Workshop.Api.Endpoints.InventoryItems;

public static class InventoryItemsEndpoints
{
    public static void MapInventoryItemsEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/inventory-items")
            .WithApiVersionSet(apiVersion)
            .WithTags("InventoryItems");

        group.MapGet("",
            async (
                [FromServices] IGetInventoryItemsUseCase getAllUseCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await getAllUseCase.Handle(Guid.NewGuid(), cancellationToken);

                return Results.Ok(result);
            }
        )
        .WithSummary("Gets all inventory items.")
        .WithDescription("Returns all inventory items registered in stock. Returns an empty array if none exist.")
        .Produces<Output>(StatusCodes.Status200OK)
        .RequireAuthorization();

        group.MapPost("",
            async (
                [FromBody] CreateInventoryItemRequest request,
                [FromServices] ICreateInventoryItemUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Created($"/api/v1/inventory-items/{result.GetResult<InventoryItemResponse>()?.Id}", result);
            }
        )
        .WithSummary("Registers a new inventory item.")
        .WithDescription("Registers a new part/supply in stock with an initial quantity on hand. Fails if an item with the same code already exists.")
        .Produces<Output>(StatusCodes.Status201Created)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AdminOnly")
        .WithValidation<CreateInventoryItemRequest>();
    }
}
