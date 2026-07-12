using Asp.Versioning;
using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Mappers.Auth;
using Fiap.Workshop.Api.Requests.Auth;
using Fiap.Workshop.Api.Validators.Auth;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Workshop.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(versionSet)
            .WithTags("Auth")
            .AllowAnonymous();

        group.MapPost("login",
            async (
                [FromBody] LoginRequest request,
                [FromServices] ILoginUseCase useCase,
                LoginRequestValidator validator,
                [FromHeader(Name = "x-correlation-id")] Guid correlationId,
                CancellationToken cancellationToken
            ) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                    return Results.Unauthorized();

                var output = await useCase.ExecuteAsync(request.MapToInput(correlationId), cancellationToken);

                if (!output.IsValid)
                    return Results.Unauthorized();

                return Results.Ok(output);
            }
        )
        .WithName("Login")
        .Produces<Output>()
        .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}
