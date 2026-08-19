using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.InventoryItems;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.InventoryItems.DeleteInventoryItem.Boundaries;
using Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItem.Boundaries;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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

        group.MapGet("{inventoryItemId}",
            async (
                [Required][FromRoute] Guid inventoryItemId,
                [FromServices] IGetInventoryItemUseCase getByIdUseCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await getByIdUseCase.Handle(new GetInventoryItemInput(Guid.NewGuid(), inventoryItemId), cancellationToken);
                if (!result.IsValid)
                    return Results.NotFound(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Gets an inventory item by id.")
        .WithDescription("Returns the inventory item that matches the given identifier.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status404NotFound)
        .RequireAuthorization();

        group.MapPut("{inventoryItemId}",
            async (
                [Required][FromRoute] Guid inventoryItemId,
                [FromBody] UpdateInventoryItemRequest request,
                [FromServices] IUpdateInventoryItemUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(inventoryItemId), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Updates an existing inventory item.")
        .WithDescription("Updates name, description, unit price, minimum stock, unit of measure and active status of an existing inventory item. The catalog code, quantity on hand and reserved quantity cannot be changed here — stock levels are only managed through the budget/approval flow. Fails if the inventory item does not exist.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AdminOnly")
        .WithValidation<UpdateInventoryItemRequest>();

        group.MapDelete("{inventoryItemId}",
            async (
                [Required][FromRoute] Guid inventoryItemId,
                [FromServices] IDeleteInventoryItemUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(new DeleteInventoryItemInput(Guid.NewGuid(), inventoryItemId), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.NoContent();
            }
        )
        .WithSummary("Deletes an existing inventory item.")
        .WithDescription("Deletes an existing inventory item by id. Idempotent: returns No Content whether the item existed or not. Fails if the item has ever been used in a service order.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AdminOnly");
    }
}
