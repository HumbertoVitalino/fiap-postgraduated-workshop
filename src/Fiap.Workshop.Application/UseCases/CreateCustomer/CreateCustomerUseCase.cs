using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.CreateCustomer.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.CreateCustomer;

public sealed class CreateCustomerUseCase(
    ICustomerRepository customerRepository,
    ILogger<CreateCustomerUseCase> logger
) : ICreateCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly ILogger<CreateCustomerUseCase> _logger = logger;

    public async Task<Output> Handle(CreateCustomerInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var isAnyCustomer = await _customerRepository.AnyAsync(input.Document, cancellationToken);
        if (isAnyCustomer)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Customer with this document already exists.",
                input.CorrelationId
            );

            output.AddErrorMessage("Customer with the provided document already exists.");
            return output;
        }

        var customer = input.MapToDomain();

        await _customerRepository.AddAsync(customer, cancellationToken);

        var saved = await _customerRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Failed to save customer to the database.",
                input.CorrelationId
            );

            output.AddErrorMessage("Failed to save customer to the database.");

            return output;
        }

        output.AddResult(customer.MapToDto());
        return output;
    }
}
