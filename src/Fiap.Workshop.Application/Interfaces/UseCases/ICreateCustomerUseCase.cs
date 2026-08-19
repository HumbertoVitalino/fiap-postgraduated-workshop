using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ICreateCustomerUseCase
{
    Task<Output> Handle(CreateCustomerInput input, CancellationToken cancellationToken);
}
