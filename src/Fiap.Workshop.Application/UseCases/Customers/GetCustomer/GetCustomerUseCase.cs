using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Mapper;
using Fiap.Workshop.Application.UseCases.Customers.GetCustomer.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Customers.GetCustomer;

public class GetCustomerUseCase(
    ICustomerRepository customerRepository,
    ILogger<GetCustomerUseCase> logger
) : IGetCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly ILogger<GetCustomerUseCase> _logger = logger;

    public async Task<Output> Handle(GetCustomerInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var customer = await _customerRepository.GetByIdAsync(input.CustomerId, cancellationToken);
        if (customer is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Unable to find customer by [{CustomerId}].",
                input.CorrelationId,
                input.CustomerId
            );

            output.AddErrorMessage("Unable to find customer");
            return output;
        }

        output.AddResult(customer.MapToDto());

        return output;
    }
}
