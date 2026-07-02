using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Mappers.Users;
using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Api.Validators.Users;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.GetUserById.Boundaries;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Workshop.Api.Endpoints.Users;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsers(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/users")
            .WithApiVersionSet(versionSet)
            .WithTags("Users")
            .RequireAuthorization("UserOnly");

        group.MapPost("",
            async (
                [FromBody] CreateUserRequest request,
                [FromServices] ICreateUserUseCase useCase,
                CreateUserRequestValidator validator,
                [FromHeader(Name = "x-correlation-id")] Guid? correlationId,
                CancellationToken cancellationToken
            ) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    Output validationOutput = new();
                    validationOutput.AddErrorMessages(validation.Errors.Select(e => e.ErrorMessage));
                    return Results.BadRequest(validationOutput);
                }

                var output = await useCase.ExecuteAsync(request.MapToInput(correlationId), cancellationToken);

                if (!output.IsValid)
                    return Results.BadRequest(output);

                return Results.Created();
            })
            .AllowAnonymous()
            .WithName("CreateUser")
            .Produces<Output>(StatusCodes.Status201Created)
            .Produces<Output>(StatusCodes.Status400BadRequest
        );

        group.MapGet("{id:guid}",
            async (
                [FromRoute] Guid id,
                [FromServices] IGetUserByIdUseCase useCase,
                [FromHeader(Name = "x-correlation-id")] Guid? correlationId,
                CancellationToken cancellationToken
            ) =>
            {
                var output = await useCase.ExecuteAsync(new GetUserByIdInput(id, correlationId ?? Guid.NewGuid()), cancellationToken);

                if (!output.IsValid)
                    return Results.NotFound(output);

                return Results.Ok(output);
            })
            .WithName("GetUserById")
            .Produces<Output>()
            .Produces<Output>(StatusCodes.Status404NotFound
        );

        return app;
    }
}
