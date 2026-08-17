using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Mapper;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.Customers.GetCustomers.Mapper;

public static class GetCustomersMapper
{
    public static IEnumerable<CustomerResponse> MapToDto(this IEnumerable<Customer> customers) => customers.Select(x => x.MapToDto());
}
