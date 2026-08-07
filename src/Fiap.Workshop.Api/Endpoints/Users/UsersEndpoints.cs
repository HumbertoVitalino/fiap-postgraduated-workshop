using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Workshop.Api.Endpoints.Users;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("/users")
            .WithApiVersionSet(apiVersion)
            .WithName("Users")
            .WithTags("Users");

        group.MapPost("",
            async (
                [FromBody] CreateUserRequest request,
                [FromServices] ICreateUserUseCase useCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await useCase.Handle(request.MapToInput(), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest();

                return Results.Created();
            }
        ).RequireAuthorization();
    }
}
