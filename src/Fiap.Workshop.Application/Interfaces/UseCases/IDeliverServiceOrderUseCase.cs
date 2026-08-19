using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.DeliverServiceOrder.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IDeliverServiceOrderUseCase
{
    Task<Output> Handle(DeliverServiceOrderInput input, CancellationToken cancellationToken);
}
