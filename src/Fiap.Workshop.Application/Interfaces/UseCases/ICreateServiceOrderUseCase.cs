using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ICreateServiceOrderUseCase
{
    Task<Output> Handle(CreateServiceOrderInput input, CancellationToken cancellationToken);
}
