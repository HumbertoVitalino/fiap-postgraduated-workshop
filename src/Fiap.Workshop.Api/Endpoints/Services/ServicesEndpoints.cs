using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.Services;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Services.DeleteService.Boundaries;
using Fiap.Workshop.Application.UseCases.Services.GetService.Boundaries;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Fiap.Workshop.Api.Endpoints.Services;

public static class ServicesEndpoints
{
    public static void MapServicesEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/services")
            .WithApiVersionSet(apiVersion)
            .WithTags("Services");

        group.MapGet("",
            async (
                [FromServices] IGetServicesUseCase getAllUseCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await getAllUseCase.Handle(Guid.NewGuid(), cancellationToken);

                return Results.Ok(result);
            }
        )
        .WithSummary("Gets all services.")
        .WithDescription("Returns all services registered in the catalog. Returns an empty array if none exist.")
        .Produces<Output>(StatusCodes.Status200OK)
        .RequireAuthorization();

        group.MapPost("",
            async (
                [FromBody] CreateServiceRequest request,
                [FromServices] ICreateServiceUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Created($"/api/v1/services/{result.GetResult<ServiceResponse>()?.Id}", result);
            }
        )
        .WithSummary("Registers a new service.")
        .WithDescription("Registers a new service in the catalog. Fails if a service with the same code already exists.")
        .Produces<Output>(StatusCodes.Status201Created)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AdminOnly")
        .WithValidation<CreateServiceRequest>();

        group.MapGet("{serviceId}",
            async (
                [Required][FromRoute] Guid serviceId,
                [FromServices] IGetServiceUseCase getByIdUseCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await getByIdUseCase.Handle(new GetServiceInput(Guid.NewGuid(), serviceId), cancellationToken);
                if (!result.IsValid)
                    return Results.NotFound(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Gets a service by id.")
        .WithDescription("Returns the service that matches the given identifier.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status404NotFound)
        .RequireAuthorization();

        group.MapPut("{serviceId}",
            async (
                [Required][FromRoute] Guid serviceId,
                [FromBody] UpdateServiceRequest request,
                [FromServices] IUpdateServiceUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(serviceId), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Updates an existing service.")
        .WithDescription("Updates name, description, base price, estimated duration and active status of an existing service. The catalog code cannot be changed. Changing the estimated duration resets the incremental execution average (ExecutionCount back to zero). Fails if the service does not exist.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AdminOnly")
        .WithValidation<UpdateServiceRequest>();

        group.MapDelete("{serviceId}",
            async (
                [Required][FromRoute] Guid serviceId,
                [FromServices] IDeleteServiceUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(new DeleteServiceInput(Guid.NewGuid(), serviceId), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.NoContent();
            }
        )
        .WithSummary("Deletes an existing service.")
        .WithDescription("Deletes an existing service by id. Idempotent: returns No Content whether the service existed or not. Fails if the service has ever been used in a service order.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AdminOnly");
    }
}
