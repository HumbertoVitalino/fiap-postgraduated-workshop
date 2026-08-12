using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.Auth;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Workshop.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/auth")
            .WithApiVersionSet(apiVersion)
            .WithName("Auth")
            .WithTags("Auth");

        group.MapPost("/login",
            async (
                [FromBody] LoginRequest request,
                [FromServices] ILoginUserUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(), cancellationToken);
                if (!result.IsValid)
                    return Results.Unauthorized();

                return Results.Ok(result);
            }
        )
        .WithSummary("Authenticates a user.")
        .WithDescription("Validates the given credentials and returns a JWT access token on success.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .AllowAnonymous()
        .WithValidation<LoginRequest>();
    }
}