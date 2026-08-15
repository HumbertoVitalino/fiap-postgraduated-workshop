using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Mapper;

public static class CreateCustomerMapper
{
    public static Customer MapToDomain(this CreateCustomerInput input)
    {
        return new(
            Guid.NewGuid(),
            input.Name,
            input.Document,
            input.Email,
            input.Phone,
            DateTime.Now,
            DateTime.Now
        );
    }

    public static CustomerResponse MapToDto(this Customer customer)
    {
        return new(
            customer.Id,
            customer.Name,
            customer.Phone
        );
    }
}
