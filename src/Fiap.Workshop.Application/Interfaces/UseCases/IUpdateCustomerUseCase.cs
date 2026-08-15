using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.UpdateCustomer.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IUpdateCustomerUseCase
{
    Task<Output> Handle(UpdateCustomerInput input, CancellationToken cancellationToken);
}
