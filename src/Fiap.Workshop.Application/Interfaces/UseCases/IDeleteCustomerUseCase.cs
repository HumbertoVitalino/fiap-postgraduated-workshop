using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Customers.DeleteCustomer.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IDeleteCustomerUseCase
{
    Task<Output> Handle(DeleteCustomerInput input, CancellationToken cancellationToken);
}
