using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.Vehicles;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Vehicles.DeleteVehicle.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicle.Boundaries;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Fiap.Workshop.Api.Endpoints.Vehicles;

public static class VehiclesEndpoints
{
    public static void MapVehiclesEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/vehicles")
            .WithApiVersionSet(apiVersion)
            .WithTags("Vehicles");

        group.MapGet("",
            async (
                [FromServices] IGetVehiclesUseCase getAllUseCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await getAllUseCase.Handle(Guid.NewGuid(), cancellationToken);

                return Results.Ok(result);
            }
        )
        .WithSummary("Gets all vehicles.")
        .WithDescription("Returns all existing vehicles, an empty array if none exist.")
        .Produces<Output>(StatusCodes.Status200OK)
        .RequireAuthorization("AttendantOnly");

        group.MapGet("{vehicleId}",
            async (
                [Required][FromRoute] Guid vehicleId,
                [FromServices] IGetVehicleUseCase getByIdUseCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await getByIdUseCase.Handle(new GetVehicleInput(Guid.NewGuid(), vehicleId), cancellationToken);
                if (!result.IsValid)
                    return Results.NotFound(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Gets a vehicle by id.")
        .WithDescription("Returns the vehicle that matches the given identifier.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status404NotFound)
        .RequireAuthorization("AttendantOnly");

        group.MapPost("",
            async (
                [FromBody] CreateVehicleRequest request,
                [FromServices] ICreateVehicleUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Created($"/api/v1/vehicles/{result.GetResult<VehicleResponse>()?.Id}", result);
            }
        )
        .WithSummary("Registers a new vehicle.")
        .WithDescription("Registers a new vehicle linked to an existing customer. Fails if the customer does not exist or the license plate already belongs to another vehicle.")
        .Produces<Output>(StatusCodes.Status201Created)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AttendantOnly")
        .WithValidation<CreateVehicleRequest>();

        group.MapPut("{vehicleId}",
            async (
                [Required][FromRoute] Guid vehicleId,
                [FromBody] UpdateVehicleRequest request,
                [FromServices] IUpdateVehicleUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(vehicleId), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Updates an existing vehicle.")
        .WithDescription("Updates brand, model, color and model year of an existing vehicle. The license plate and manufacture year cannot be changed. Fails if the vehicle does not exist.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AttendantOnly")
        .WithValidation<UpdateVehicleRequest>();

        group.MapDelete("{vehicleId}",
            async (
                [Required][FromRoute] Guid vehicleId,
                [FromServices] IDeleteVehicleUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(new DeleteVehicleInput(Guid.NewGuid(), vehicleId), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.NoContent();
            }
        )
        .WithSummary("Deletes an existing vehicle.")
        .WithDescription("Deletes an existing vehicle by id. Idempotent: returns No Content whether the vehicle existed or not. Fails if the vehicle has service orders associated.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AttendantOnly");
    }
}
