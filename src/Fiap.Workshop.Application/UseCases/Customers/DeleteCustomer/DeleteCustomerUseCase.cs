using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.DeleteCustomer.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Customers.DeleteCustomer;

public sealed class DeleteCustomerUseCase(
    ICustomerRepository customerRepository,
    IVehicleRepository vehicleRepository,
    ILogger<DeleteCustomerUseCase> logger
) : IDeleteCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly IVehicleRepository _vehicleRepository = vehicleRepository;
    private readonly ILogger<DeleteCustomerUseCase> _logger = logger;

    public async Task<Output> Handle(DeleteCustomerInput input, CancellationToken cancellationToken)
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

            return output;
        }

        var hasVehicles = await _vehicleRepository.ExistsWithCustomerIdAsync(customer.Id, cancellationToken);
        if (hasVehicles)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Customer with id {CustomerId} has vehicles associated and cannot be deleted.",
                input.CorrelationId,
                input.CustomerId
            );

            output.AddErrorMessage("Customer has vehicles associated and cannot be deleted.");
            return output;
        }

        _customerRepository.Remove(customer);

        var saved = await _customerRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error deleting customer with id {CustomerId}.",
                input.CorrelationId,
                input.CustomerId
            );

            output.AddErrorMessage($"Error deleting customer with id {input.CustomerId}.");
            return output;
        }

        return output;
    }
}
