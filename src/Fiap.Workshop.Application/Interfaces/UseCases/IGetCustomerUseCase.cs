using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.GetCustomer.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetCustomerUseCase
{
    Task<Output> Handle(GetCustomerInput input, CancellationToken cancellationToken);
}
