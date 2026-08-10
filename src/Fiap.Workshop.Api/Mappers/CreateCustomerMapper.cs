using Fiap.Workshop.Api.Requests.Customers;
using Fiap.Workshop.Application.UseCases.CreateCustomer.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class CreateCustomerMapper
{
    public static CreateCustomerInput MapToInput(this CreateCustomerRequest request)
    {
        return new(
            request.CorrelationId,
            request.Name,
            request.Document,
            request.Email,
            request.Phone
        );
    }
}
