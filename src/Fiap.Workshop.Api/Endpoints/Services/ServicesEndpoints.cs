using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.Services;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Workshop.Api.Endpoints.Services;

public static class ServicesEndpoints
{
    public static void MapServicesEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/services")
            .WithApiVersionSet(apiVersion)
            .WithTags("Services");

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
    }
}
