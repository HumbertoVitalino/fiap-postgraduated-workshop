using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrder.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetServiceOrderUseCase
{
    Task<Output> Handle(GetServiceOrderInput input, CancellationToken cancellationToken);
}
