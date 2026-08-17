using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.GetCustomers.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Customers.GetCustomers;

public sealed class GetCustomersUseCase(
    ICustomerRepository customerRepository,
    ILogger<GetCustomersUseCase> logger
) : IGetCustomersUseCase
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly ILogger<GetCustomersUseCase> _logger = logger;

    public async Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken)
    {
        Output output = new();

        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        if (!customers.Any())
        {
            _logger.LogWarning("[{CorrelationId}] | Unable to find customers", correlationId);

            output.AddResult(Array.Empty<CustomerResponse>());
            return output;
        }

        output.AddResult(customers.MapToDto());
        return output;
    }
}
