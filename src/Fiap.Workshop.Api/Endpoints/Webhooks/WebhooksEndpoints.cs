using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.Webhooks;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Workshop.Api.Endpoints.Webhooks;

public static class WebhooksEndpoints
{
    public static void MapWebhooksEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/webhooks")
            .WithApiVersionSet(apiVersion)
            .WithTags("Webhooks");

        group.MapPost("budget-decisions",
            async (
                [FromBody] BudgetDecisionWebhookRequest request,
                [FromServices] IProcessBudgetDecisionUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Receives the customer's budget decision from an external system.")
        .WithDescription("Called by an external system (authenticated with the X-Api-Key header, not JWT) when the customer approves or rejects the budget of a service order in AwaitingApproval. Approved (0) commits the reserved inventory stock and moves the order to InProgress; Rejected (1) releases the reservation and moves it to Cancelled. The change is recorded under the system user. Idempotent: replaying the same decision returns 200 with the message \"Budget decision already applied\" and no side effects; a contradicting decision for an order already decided returns 400.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .AllowAnonymous()
        .WithApiKey()
        .WithValidation<BudgetDecisionWebhookRequest>();
    }
}
