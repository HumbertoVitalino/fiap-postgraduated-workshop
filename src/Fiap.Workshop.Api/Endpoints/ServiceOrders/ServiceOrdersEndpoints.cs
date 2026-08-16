using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrder.Boundaries;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Fiap.Workshop.Api.Endpoints.ServiceOrders;

public static class ServiceOrdersEndpoints
{
    public static void MapServiceOrdersEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/service-orders")
            .WithApiVersionSet(apiVersion)
            .WithTags("ServiceOrders");

        group.MapGet("{serviceOrderId}",
            async (
                [Required][FromRoute] Guid serviceOrderId,
                [FromServices] IGetServiceOrderUseCase getByIdUseCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await getByIdUseCase.Handle(new GetServiceOrderInput(Guid.NewGuid(), serviceOrderId), cancellationToken);
                if (!result.IsValid)
                    return Results.NotFound(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Gets a service order by id.")
        .WithDescription("Returns the service order that matches the given identifier, including its status, budget items and status history.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status404NotFound)
        .RequireAuthorization();

        group.MapPost("",
            async (
                [FromBody] CreateServiceOrderRequest request,
                [FromServices] ICreateServiceOrderUseCase useCase,
                [FromServices] ICurrentUserService currentUser,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(currentUser.UserId), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Created($"/api/v1/service-orders/{result.GetResult<ServiceOrderResponse>()?.Id}", result);
            }
        )
        .WithSummary("Opens a new service order.")
        .WithDescription("Opens a service order for an existing vehicle owned by an existing customer. The order starts in the Received status with no budget.")
        .Produces<Output>(StatusCodes.Status201Created)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AttendantOnly")
        .WithValidation<CreateServiceOrderRequest>();

        group.MapPatch("{serviceOrderId}/diagnosis",
            async (
                [Required][FromRoute] Guid serviceOrderId,
                [FromBody] StartDiagnosisRequest request,
                [FromServices] IStartDiagnosisUseCase useCase,
                [FromServices] ICurrentUserService currentUser,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(serviceOrderId, currentUser.UserId), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Starts the diagnosis of a service order.")
        .WithDescription("Registers the diagnosis description and moves the service order from Received to Diagnosing.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("MechanicOnly")
        .WithValidation<StartDiagnosisRequest>();

        group.MapPatch("{serviceOrderId}/budget",
            async (
                [Required][FromRoute] Guid serviceOrderId,
                [FromBody] AddBudgetRequest request,
                [FromServices] IAddBudgetUseCase useCase,
                [FromServices] ICurrentUserService currentUser,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(serviceOrderId, currentUser.UserId), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Adds a budget to a service order.")
        .WithDescription("Calculates the subtotal/total from the given services and parts, reserves the requested inventory stock, and moves the service order from Diagnosing to AwaitingApproval.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("MechanicOnly")
        .WithValidation<AddBudgetRequest>();

        group.MapPatch("{serviceOrderId}/approve",
            async (
                [Required][FromRoute] Guid serviceOrderId,
                [FromBody] Guid request,
                [FromServices] IApproveServiceOrderUseCase useCase,
                [FromServices] ICurrentUserService currentUser,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(serviceOrderId, currentUser.UserId), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Approves the budget of a service order.")
        .WithDescription("Registers the attendant's record of the customer's approval, commits the reserved inventory stock, and moves the service order from AwaitingApproval to InProgress.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AttendantOnly");
    }
}
