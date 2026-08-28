using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Mapper;
using Fiap.Workshop.Application.UseCases.Customers.UpdateCustomer.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Customers.UpdateCustomer;

public sealed class UpdateCustomerUseCase(
    ICustomerRepository customerRepository,
    ILogger<UpdateCustomerUseCase> logger
) : IUpdateCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly ILogger<UpdateCustomerUseCase> _logger = logger;

    public async Task<Output> Handle(UpdateCustomerInput input, CancellationToken cancellationToken)
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

        if (!string.Equals(customer.Email, input.Email, StringComparison.Ordinal))
        {
            var emailInUse = await _customerRepository.ExistsWithEmailAsync(input.Email, cancellationToken);
            if (emailInUse)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] | Customer with email {Email} already exists.",
                    input.CorrelationId,
                    input.Email.MaskEmail()
                );

                output.AddErrorMessage($"Customer with email {input.Email} already exists.");
                return output;
            }
        }

        if (!string.Equals(customer.Phone, input.Phone, StringComparison.Ordinal))
        {
            var phoneInUse = await _customerRepository.ExistsWithPhoneAsync(input.Phone, cancellationToken);
            if (phoneInUse)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] | Customer with phone {Phone} already exists.",
                    input.CorrelationId,
                    input.Phone.SanitizeForLog()
                );

                output.AddErrorMessage($"Customer with phone {input.Phone} already exists.");
                return output;
            }
        }

        customer.UpdateProfile(input.Name, input.Email, input.Phone);

        _customerRepository.Update(customer);

        var saved = await _customerRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error updating customer with id {CustomerId}.",
                input.CorrelationId,
                input.CustomerId
            );

            output.AddErrorMessage($"Error updating customer with id {input.CustomerId}.");
            return output;
        }

        output.AddResult(customer.MapToDto());
        return output;
    }
}
