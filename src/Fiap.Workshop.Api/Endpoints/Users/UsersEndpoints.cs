using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Workshop.Api.Endpoints.Users;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/users")
            .WithApiVersionSet(apiVersion)
            .WithTags("Users");

        group.MapGet("",
            async (
                [FromServices] IGetUsersUseCase getAllUseCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await getAllUseCase.Handle(Guid.NewGuid(), cancellationToken);

                return Results.Ok(result);
            }

        )
        .WithSummary("Get all users.")
        .WithDescription("Get all existing users, return OK array empty if dont exist any user")
        .Produces<Output>(StatusCodes.Status200OK)
        .RequireAuthorization("AdminOnly");

        group.MapPost("",
            async (
                [FromBody] CreateUserRequest request,
                [FromServices] ICreateUserUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Created($"/api/v1/users/{result.GetResult<UserResponse>()?.Id}", result);
            }
        )
        .WithSummary("Creates a new user.")
        .WithDescription("Creates a new internal user account. Fails if the request data is invalid or a user with the same email already exists.")
        .Produces<Output>(StatusCodes.Status201Created)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AdminOnly")
        .WithValidation<CreateUserRequest>();
    }
}
