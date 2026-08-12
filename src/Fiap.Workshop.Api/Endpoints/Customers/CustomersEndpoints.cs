using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.Customers;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Workshop.Api.Endpoints.Customers;

public static class CustomersEndpoints
{
    public static void MapCustomersEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/customers")
            .WithApiVersionSet(apiVersion)
            .WithName("Customers")
            .WithTags("Customers");

        group.MapPost("",
            async (
                [FromBody] CreateCustomerRequest request,
                [FromServices] ICreateCustomerUseCase createUseCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await createUseCase.Handle(request.MapToInput(), cancellationToken);
                if (!result.IsValid)
                    return Results.BadRequest(result);

                return Results.Created($"/api/v1/customers/{result.GetResult<CustomerResponse>()?.Id}", result);
            }
        )
        .RequireAuthorization("AttendantOnly")
        .WithValidation<CreateCustomerRequest>();
    }
}
