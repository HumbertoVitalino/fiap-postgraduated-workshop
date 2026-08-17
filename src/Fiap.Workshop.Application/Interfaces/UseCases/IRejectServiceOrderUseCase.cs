using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.RejectServiceOrder.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IRejectServiceOrderUseCase
{
    Task<Output> Handle(RejectServiceOrderInput input, CancellationToken cancellationToken);
}
