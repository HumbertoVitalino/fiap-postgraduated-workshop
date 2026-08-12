using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Filters;
using Fiap.Workshop.Api.Mappers;
using Fiap.Workshop.Api.Requests.Customers;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.GetCustomer.Boundaries;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Fiap.Workshop.Api.Endpoints.Customers;

public static class CustomersEndpoints
{
    public static void MapCustomersEndpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersion)
    {
        var group = app.MapGroup("api/v1/customers")
            .WithApiVersionSet(apiVersion)
            .WithTags("Customers");

        group.MapGet("{customerId}",
            async (
                [Required][FromRoute] Guid customerId,
                [FromServices] IGetCustomerUseCase getByIdUseCase,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await getByIdUseCase.Handle(new GetCustomerInput(Guid.NewGuid(), customerId), cancellationToken);
                if (!result.IsValid)
                    return Results.NotFound(result);

                return Results.Ok(result);
            }
        )
        .WithSummary("Gets a customer by id.")
        .WithDescription("Returns the customer that matches the given identifier.")
        .Produces<Output>(StatusCodes.Status200OK)
        .Produces<Output>(StatusCodes.Status404NotFound)
        .RequireAuthorization("AttendantOnly");

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
        .WithSummary("Creates a new customer.")
        .WithDescription("Creates a new customer record. Fails if the request data is invalid or a customer with the same document already exists.")
        .Produces<Output>(StatusCodes.Status201Created)
        .Produces<Output>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AttendantOnly")
        .WithValidation<CreateCustomerRequest>();
    }
}
