using Fiap.Workshop.Api.Requests.Customers;
using Fiap.Workshop.Application.UseCases.Customers.UpdateCustomer.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class UpdateCustomerMapper
{
    public static UpdateCustomerInput MapToInput(this UpdateCustomerRequest request, Guid customerId)
    {
        return new(
            request.CorrelationId,
            customerId,
            request.Name,
            request.Email,
            request.Phone
        );
    }
}
