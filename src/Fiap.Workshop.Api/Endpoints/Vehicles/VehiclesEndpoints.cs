using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.Vehicles;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Workshop.Api.Endpoints.Vehicles;

public static class VehiclesEndpoints
{
    public static void MapVehiclesEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/vehicles")
            .WithApiVersionSet(apiVersion)
            .WithTags("Vehicles");

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
    }
}
